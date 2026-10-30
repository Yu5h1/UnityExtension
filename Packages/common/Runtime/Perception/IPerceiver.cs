using System;

namespace Yu5h1Lib
{
    /// <summary>Something that looks: a scroll view's viewport, a camera. <see cref="Perceiver"/> is the scene component.</summary>
    public interface IPerceiver
    {
        bool CanSee(IPerceptible target);
    }

    /// <summary>
    /// Something that can be seen. A perceiver calls <see cref="Spotted"/> and <see cref="LostSight"/> when its view
    /// of the target changes; the target raises its events from them (an interface cannot let others raise its events).
    /// </summary>
    public interface IPerceptible
    {
        event Action<IPerceiver> OnSpotted;
        event Action<IPerceiver> OnLostSight;
        void Spotted(IPerceiver perceiver);
        void LostSight(IPerceiver perceiver);
    }

    /// <summary>How a perceiver decides that it sees a target: viewport overlap, camera frustum, distance.</summary>
    public interface ISightStrategy
    {
        bool Check(IPerceiver perceiver, IPerceptible target);
    }
}
