using Unity.Netcode.Components;

namespace OutOfSync.Networking
{
    /// <summary>
    /// Player movement is owner-authoritative. The local player drives its own
    /// transform while NGO replicates it to the other clients.
    /// </summary>
    public sealed class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
