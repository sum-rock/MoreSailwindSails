// Owns one winch's binding, placement, visibility, replacement and final disposal.
using System;
using MoreSailwindSails.Controls.Placement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreSailwindSails.Controls.Runtime
{
    internal sealed class OwnedWinch : IDisposable
    {
        private readonly WinchControls manager;
        private readonly BoatRefs boat;
        internal readonly WinchGroup Group;
        internal readonly WinchRole Role;
        private readonly Transform parent;
        private readonly string label;
        private WinchClone clone;
        private bool disposed;
        internal GameObject Owner => Group.Owner;
        internal GPButtonRopeWinch Source => clone.Source;
        internal GPButtonRopeWinch Winch => clone.Winch;
        internal bool Wanted =>
            !disposed && Owner && Owner.activeInHierarchy && Winch && Winch.rope;

        internal OwnedWinch(
            WinchControls manager,
            BoatRefs boat,
            WinchGroup group,
            Transform parent,
            GPButtonRopeWinch source,
            WinchRole role,
            string label
        )
        {
            this.manager = manager;
            this.boat = boat;
            Group = group;
            Role = role;
            this.parent = parent;
            this.label = label;
            clone = Prepare(source: source);
        }

        internal WinchClone Prepare(GPButtonRopeWinch source)
        {
            var root = new GameObject(label + " mount");
            root.SetActive(false);
            root.transform.SetParent(parent: parent, worldPositionStays: false);
            var prepared = new WinchClone { Source = source, Mount = root.transform };
            try
            {
                var obj = Object.Instantiate(
                    original: source.gameObject,
                    parent: prepared.Mount,
                    worldPositionStays: false
                );
                obj.name = label;
                prepared.Winch = obj.GetComponent<GPButtonRopeWinch>();
                prepared.Winch.rope = null;
                obj.AddComponent<OwnedWinchMarker>();
                WinchVisuals.ResetClonedOutline(winch: prepared.Winch);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                if (
                    prepared.Winch.rotHandle
                    && !prepared.Winch.rotHandle.transform.IsChildOf(obj.transform)
                )
                {
                    prepared.Winch.rotHandle = Object.Instantiate(
                        original: prepared.Winch.rotHandle,
                        parent: obj.transform,
                        worldPositionStays: false
                    );
                    prepared.Winch.rotHandle.transform.localPosition = Vector3.zero;
                }
                if (prepared.Winch.rotHandle)
                    prepared.Winch.rotHandle.rotatable = obj.transform;
                obj.SetActive(true);
                return prepared;
            }
            catch
            {
                // No controller has been attached to this uncommitted clone.
                Object.Destroy(root);
                throw;
            }
        }

        internal void Adopt(WinchClone replacement)
        {
            var controller = Winch ? Winch.rope : null;
            var previous = clone;
            previous.Detach(boat: boat);
            clone = replacement;
            // Preserve the binding even if AttachToController throws while updating visuals.
            Winch.rope = controller;
            Group.Publish();
            previous.Destroy();
            if (controller)
                Bind(controller: controller);
        }

        internal void Bind(RopeController controller)
        {
            try
            {
                Winch.AttachToController(controller: controller);
                NativeBindingComplete();
            }
            catch
            {
                Group.Suspend();
                throw;
            }
        }

        internal void NativeBindingComplete()
        {
            // Native binding reparents ropes beneath the winch. Hidden placement
            // mounts must not deactivate controllers during exhaustion or support loss.
            if (clone.Mount && !clone.Mount.gameObject.activeInHierarchy)
                clone.Detach(boat: boat);
            Group.Dirty();
        }

        internal void Position(WinchSeat seat)
        {
            clone.Mount.SetPositionAndRotation(
                position: boat.transform.TransformPoint(position: seat.Position),
                rotation: boat.transform.rotation * seat.Rotation
            );
            var scale = clone.Mount.lossyScale;
            var sourceScale = Source.transform.lossyScale;
            Winch.transform.localScale = new Vector3(
                sourceScale.x / scale.x,
                sourceScale.y / scale.y,
                sourceScale.z / scale.z
            );
            // Input is wheel-local: move only its parent mounting frame.
            if (Winch.rope && Winch.rope.transform.parent != clone.Mount)
                Winch.AttachToController(controller: Winch.rope);
        }

        internal void Show()
        {
            if (!clone.Mount.gameObject.activeSelf)
                clone.Mount.gameObject.SetActive(true);
            if (!Winch.GetComponent<Renderer>().enabled || !Winch.GetComponent<Collider>().enabled)
                Winch.ShowWinch(true);
        }

        internal void Suspend(bool preserveControllers)
        {
            if (preserveControllers)
                clone.Detach(boat: boat);
            if (clone.Mount)
                clone.Mount.gameObject.SetActive(false);
        }

        internal void Retire()
        {
            if (disposed)
                return;
            Group.Suspend(preserveControllers: true);
            clone.Detach(boat: boat);
            Dispose();
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            Group.Remove(control: this);
            clone.Destroy();
            manager.Remove(control: this);
        }
    }
}
