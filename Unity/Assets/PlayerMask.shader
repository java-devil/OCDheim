Shader "OCDHeim/PlayerMask"
{
    SubShader
    {
        Tags { "Queue"="Geometry" }

        Pass
        {
            ColorMask 0
            ZWrite Off
            Offset -2, -2

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
        }
    }
}
