using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;

namespace D2.Game
{
    /// <summary>
    /// Track which effect parameters need to be recomputed during the next OnApply.
    /// </summary>
    [Flags]
    internal enum EffectDirtyFlags
    {
        WorldViewProj   = 1,
        World           = 2,
        EyePosition     = 4,
        MaterialColor   = 8,
        Fog             = 16,
        FogEnable       = 32,
        AlphaTest       = 64,
        ShaderIndex     = 128,
        All             = -1
    }


    /// <summary>
    /// Helper code shared between the various built-in effects.
    /// </summary>
    internal static class EffectHelpers
    {
        /// <summary>
        /// Lazily recomputes the world+view+projection matrix and
        /// fog vector based on the current effect parameter settings.
        /// </summary>
        internal static EffectDirtyFlags SetWorldViewProj(EffectDirtyFlags dirtyFlags,
                                                                ref Matrix world, ref Matrix view, ref Matrix projection, ref Matrix worldView,
                                                                EffectParameter worldViewProjParam)
        {
            // Recompute the world+view+projection matrix?
            //if ((dirtyFlags & EffectDirtyFlags.WorldViewProj) != 0)
            {
                Matrix worldViewProj;
                
                Matrix.Multiply(ref world, ref view, out worldView);
                Matrix.Multiply(ref worldView, ref projection, out worldViewProj);
                
                worldViewProjParam.SetValue(worldViewProj);
                
            //    dirtyFlags &= ~EffectDirtyFlags.WorldViewProj;
            }

            return dirtyFlags;
        }


        
    }
}