#if !UNITY_4_7 && !UNITY_5_0 && !PROTOTYPE

using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using ProBuilder.Core;
using System;
using ProBuilder.EditorCore;

namespace ProBuilder.Test
{
	public class TestSelectionRenderState
	{
		[Test]
		public static void TestSelectionRenderStateMatchesUnity()
		{
			Assert.AreEqual( (int) SelectionRenderState.None, (int) EditorSelectedRenderState.Hidden );
			Assert.AreEqual( (int) SelectionRenderState.Wireframe, (int) EditorSelectedRenderState.Wireframe );
			Assert.AreEqual( (int) SelectionRenderState.Outline, (int) EditorSelectedRenderState.Highlight );
		}
	}
}
#endif
