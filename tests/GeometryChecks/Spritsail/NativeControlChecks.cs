using System;
using MoreSailwindSails.Sails.Spritsail;

namespace MoreSailwindSails.Tests.GeometryChecks.Spritsail;

// Exercises indexed native fitting capacity and shared winch identity without Unity objects.
internal static class NativeControlChecks
{
    internal static void Run()
    {
        var port = new[] { new object(), new object() };
        var starboard = new[] { new object(), new object() };
        var reef = new[] { new object(), new object() };
        var middle = new[] { new object(), new object() };
        object[] Loose(int order) =>
            new[]
            {
                SpritsailControlSlots.At(items: reef, index: order),
                SpritsailControlSlots.At(
                    items: port,
                    index: SpritsailControlSlots.SheetIndex(square: false, order: order)
                ),
                SpritsailControlSlots.At(
                    items: starboard,
                    index: SpritsailControlSlots.SheetIndex(square: false, order: order)
                ),
            };
        Check(
            !SpritsailControlSlots.Conflict(
                requested: Loose(order: 1),
                others: new[] { reef[0], middle[0] }
            ),
            "Gaff and loose-footed sails should use independent native slots."
        );
        Check(
            !SpritsailControlSlots.Conflict(
                requested: Loose(order: 1),
                others: new[] { reef[0], port[0], starboard[0] }
            ),
            "A loose-footed sail above a square sail must retain independent sheets."
        );
        int squareSheet = SpritsailControlSlots.SheetIndex(square: true, order: 1);
        Check(
            SpritsailControlSlots.Conflict(
                requested: Loose(order: 0),
                others: new[] { reef[1], port[squareSheet], starboard[squareSheet] }
            ),
            "A square sail above a loose-footed sail still claims pair zero: reject that collision."
        );
        Check(
            SpritsailControlSlots.Conflict(
                requested: new[] { reef[0], port[0], port[0] },
                others: Array.Empty<object>()
            ),
            "A left/right alias cannot supply two independent controls."
        );
        Check(
            SpritsailControlSlots.Conflict(requested: Loose(order: 1), others: new[] { port[1] }),
            "Shared references across native roles must be detected."
        );
        Check(
            SpritsailControlSlots.At(items: port, index: 2) == null
                && SpritsailControlSlots.At(items: port, index: -1) == null
                && SpritsailControlSlots.At<object>(items: null, index: 0) == null,
            "Missing slots must fail without indexing an invalid array."
        );
        Check(
            ReferenceEquals(SpritsailControlSlots.At(items: port, index: 1), port[1]),
            "Use the authored native slot without borrowing or cloning."
        );
        Console.WriteLine(
            "PASS: native spritsail slots, gaff/square mixed controls, square pair-zero conflicts, aliases and missing capacity."
        );
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message: message);
    }
}
