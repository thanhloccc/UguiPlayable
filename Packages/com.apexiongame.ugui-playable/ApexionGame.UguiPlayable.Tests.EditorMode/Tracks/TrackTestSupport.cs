using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

/// <summary>
/// Helpers shared by the track fixtures.
/// </summary>
internal static class TrackTestSupport
{
    /// <summary>
    /// Rebuilds the director's graph, warms it up, then returns the bytes allocated by 100 graph evaluations.
    /// </summary>
    public static long MeasureEvaluateAllocation(PlayableDirector director)
    {
        director.RebuildGraph();
        var graph = director.playableGraph;
        var root = graph.GetRootPlayable(0);

        for (var i = 0; i < 10; i++)
        {
            root.SetTime(i * 0.2d);
            graph.Evaluate();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 100; i++)
        {
            root.SetTime(i % 20 * 0.1d);
            graph.Evaluate();
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public sealed class RecordingCollector : IPropertyCollector
    {
        public readonly List<string> Names = new();

        public void PushActiveGameObject(GameObject gameObject) { }

        public void PopActiveGameObject() { }

        public void AddFromClip(AnimationClip clip) { }

        public void AddFromClips(IEnumerable<AnimationClip> clips) { }

        public void AddFromName<T>(string name) where T : Component => Names.Add(name);

        public void AddFromName(string name) => Names.Add(name);

        public void AddFromClip(GameObject obj, AnimationClip clip) { }

        public void AddFromClips(GameObject obj, IEnumerable<AnimationClip> clips) { }

        public void AddFromName<T>(GameObject obj, string name) where T : Component => Names.Add(name);

        public void AddFromName(GameObject obj, string name) => Names.Add(name);

        public void AddFromName(Component component, string name) => Names.Add(name);

        public void AddFromComponent(GameObject obj, Component component) { }

        public void AddObjectProperties(Object obj, AnimationClip clip) { }
    }
}
