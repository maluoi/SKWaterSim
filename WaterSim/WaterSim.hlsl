#include <stereokit.hlsli>

//--colorA:color = 1,1,1,1
//--colorB:color = 0,0,0,1

float4       heightMask;
float4       colorMask;
float4       colorA;
float4       colorB;

Texture2D    diffuse   : register(t0);
SamplerState diffuse_s : register(s0);

struct vsIn {
	float4 pos  : SV_POSITION;
	float3 norm : NORMAL0;
	float2 uv   : TEXCOORD0;
	float4 col  : COLOR0;
};
struct psIn : sk_ps_input_t {
	float4 pos   : SV_POSITION;
	float2 uv    : TEXCOORD0;
	float4 color : COLOR0;
};

// Note, this is a v0.4 shader, which is slightly different from the v0.3 shaders!

psIn vs(vsIn input, sk_vs_input_t sk_in) {
	psIn o;
	uint view_id = sk_view_init(sk_in, o);
	uint id      = sk_inst_id  (sk_in);

	float4 terrain = diffuse.SampleLevel(diffuse_s, input.uv, 0);

	input.pos.y += dot(terrain, heightMask);
	float4 world = mul(input.pos, sk_inst[id].world);
	o.pos        = mul(world,     sk_viewproj[view_id]);

	o.uv    = input.uv;
	o.color = lerp(colorA, colorB, dot(terrain, colorMask));
	return o;
}

float4 ps(psIn input) : SV_TARGET {
	return input.color;
}