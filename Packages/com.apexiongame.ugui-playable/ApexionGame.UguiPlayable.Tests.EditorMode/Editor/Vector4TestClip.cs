using System;
using System.Collections.Generic;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Editor;

/// <summary>A test-only behaviour with one Vector4 channel, for the inspector's Vector4 support.</summary>
[Serializable]
public sealed class Vector4TestBehaviour : TweenBehaviour
{
    [SerializeField]
    private TweenChannel<Vector4> _uvRect = new(Vector4.zero, Vector4.one);

    public TweenChannel<Vector4> UvRect => _uvRect;

    public override void CollectPropertyNames(ICollection<string> names)
    {
    }
}

/// <summary>A test-only clip holding <see cref="Vector4TestBehaviour"/>.</summary>
public sealed class Vector4TestClip : TweenClip<Vector4TestBehaviour>
{
}
