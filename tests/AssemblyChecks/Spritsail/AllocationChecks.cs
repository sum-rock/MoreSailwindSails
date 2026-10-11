using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using MoreSailwindSails.Tests.AssemblyChecks.Shared;

namespace MoreSailwindSails.Tests.AssemblyChecks.Spritsail;

// Guards recurring allocations and exercises corner arithmetic without constructing Unity objects.
internal static class AllocationChecks
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (string family in new[] { "LooseFootedSpritsail", "BoomedSpritsail" })
        {
            string prefix = $"MoreSailwindSails.Sails.Spritsail.{family}.{family}";
            var rig = assembly.GetType(name: prefix + "Rig", throwOnError: true);
            var method = rig.GetMethod(name: "ScaledCorners", bindingAttr: flags);
            var buffer = rig.GetField(name: "scaledCorners", bindingAttr: flags);
            Require(
                value: buffer.IsPrivate
                    && buffer.IsInitOnly
                    && !buffer.IsStatic
                    && !buffer.CustomAttributes.Any(a => a.AttributeType.Name == "SerializeField"),
                message: "Corner scratch storage must belong to an individual rig, outside Unity serialization."
            );
            var constructor = rig.GetConstructors(bindingAttr: flags).Single();
            Require(
                value: IlReader
                    .Instructions(method: constructor)
                    .Any(i => i.Code == OpCodes.Stfld && Equals(i.Operand, buffer)),
                message: "Every rig constructor must initialize its own scratch buffer."
            );
            Require(
                value: !IlReader
                    .Instructions(method: method)
                    .Any(i => i.Code == OpCodes.Newarr || i.Code == OpCodes.Box)
                    && IlReader.CalledMethods(method: method).All(m => m.Name == "Scale"),
                message: "Scaling corners must only update the existing buffer, without allocating or calling back into the rig."
            );

            // Skip MonoBehaviour's native constructor; only invoke the pure arithmetic method.
            var first = RuntimeHelpers.GetUninitializedObject(type: rig);
            var second = RuntimeHelpers.GetUninitializedObject(type: rig);
            var vector = buffer.FieldType.GetElementType();
            object Vector(float x, float y, float z) =>
                Activator.CreateInstance(type: vector, args: new object[] { x, y, z });
            var corners = Array.CreateInstance(elementType: vector, length: 4);
            for (int i = 0; i < 4; i++)
                corners.SetValue(value: Vector(x: i - 2, y: i + 1, z: 2 - i), index: i);
            var firstBuffer = Array.CreateInstance(elementType: vector, length: 4);
            var secondBuffer = Array.CreateInstance(elementType: vector, length: 4);
            buffer.SetValue(obj: first, value: firstBuffer);
            buffer.SetValue(obj: second, value: secondBuffer);
            var source = rig.GetField(name: "Corners", bindingAttr: flags);
            source.SetValue(obj: first, value: corners);
            source.SetValue(obj: second, value: corners);
            var result = method.Invoke(obj: first, parameters: new[] { Vector(x: 2, y: 3, z: 4) });
            Require(
                value: ReferenceEquals(result, firstBuffer),
                message: "Scaling must return the rig's scratch storage."
            );
            for (int i = 0; i < 4; i++)
                Require(
                    value: firstBuffer
                        .GetValue(index: i)
                        .Equals(Vector(x: (i - 2) * 2, y: (i + 1) * 3, z: (2 - i) * 4)),
                    message: "All four corners must retain component-wise scaling."
                );
            result = method.Invoke(obj: second, parameters: new[] { Vector(x: 1, y: 1, z: 1) });
            Require(
                value: ReferenceEquals(result, secondBuffer)
                    && firstBuffer.GetValue(index: 0).Equals(Vector(x: -4, y: 3, z: 8)),
                message: "A second rig cannot overwrite the first rig's corners."
            );
            result = method.Invoke(obj: first, parameters: new[] { Vector(x: 1, y: 1, z: 1) });
            Require(
                value: ReferenceEquals(result, firstBuffer)
                    && firstBuffer.GetValue(index: 0).Equals(corners.GetValue(index: 0)),
                message: "A changed scale must recompute values while reusing storage."
            );

            var mount = assembly.GetType(name: prefix + "Mount", throwOnError: true);
            var active = mount.GetProperty(name: "Active", bindingAttr: flags).GetMethod;
            var calls = IlReader.CalledMethods(method: active).ToArray();
            Require(
                value: !calls.Any(m =>
                    m.DeclaringType == typeof(Enumerable) || m.Name == "GetEnumerator"
                )
                    && !IlReader
                        .Instructions(method: active)
                        .Any(i => i.Code == OpCodes.Newarr || i.Code == OpCodes.Box),
                message: "Support checks must not allocate LINQ enumerators or temporary arrays."
            );
            Require(
                value: calls.Count(m => m.Name == "get_activeInHierarchy") == 4,
                message: "Support checks must retain mast, both guides and supporting-part activity checks."
            );
        }
        Console.WriteLine(
            "PASS: spritsail corner arithmetic/reuse and allocation guards; support activity checks retained. Unity cloning and support lifecycle require in-game validation."
        );
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message: message);
    }
}
