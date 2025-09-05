using System.Numerics;

static class Noise
{
	public static Vector2 Fract(this Vector2 v) => new Vector2(v.X-(int)v.X, v.Y-(int)v.Y);
	public static Vector2 Floor(this Vector2 v) => new Vector2((int)v.X, (int)v.Y);

	// Based on Squirrel's talk here:
	// https://www.gdcvault.com/play/1024365/Math-for-Game-Programmers-Noise
	public static uint Hash(int x, uint seed)
	{
		const uint BIT_NOISE1 = 0x68E31DA4;
		const uint BIT_NOISE2 = 0xB5297A4D;
		const uint BIT_NOISE3 = 0x1B56C4E9;

		uint mangled = (uint)x;
		mangled *= BIT_NOISE1;
		mangled += seed;
		mangled ^= (mangled >> 8);
		mangled += BIT_NOISE2;
		mangled ^= (mangled << 8);
		mangled *= BIT_NOISE3;
		mangled ^= (mangled >> 8);
		return mangled;
	}

	public static uint Hash(int x, int y, uint seed) {
		const int PRIME_NUMBER = 198491317;
		return Hash(x + (y * PRIME_NUMBER), seed);
	}

	public static float HashF(int x, uint seed)
		=> Hash(x, seed) / (float)uint.MaxValue;

	public static float HashF(int x, int y, uint seed)
		=> Hash(x,y,seed) / (float)uint.MaxValue;

	public struct Seed
	{
		public uint seed;
		public int  seed_curr;
		public Seed(uint seed) { this.seed = seed; seed_curr = 0; }
	}
	public static Seed  NextSeed;
	public static float NextF => Hash(NextSeed.seed_curr++, NextSeed.seed) / (float)uint.MaxValue;
	public static uint  Next  => Hash(NextSeed.seed_curr++, NextSeed.seed);
	public static int   NextRange (int   min, int   max)  => min+(int)(Hash (NextSeed.seed_curr++, NextSeed.seed)%(max-min));
	public static float NextRangeF(float min, float max)  => min+     (HashF(NextSeed.seed_curr++, NextSeed.seed)*(max-min));

	// From iQ https://www.shadertoy.com/view/XdXGW8
	static Vector2 HashDir(Vector2 pt)
	{
		Vector2 k = new Vector2( 0.3183099f, 0.3678794f );
		float a = pt.X * k.X + k.Y;
		float b = pt.Y * k.Y + k.X;
		float m = a * b*(a + b);
		m = m - (int)m;
		k = (k * (m * 16)).Fract();
		return new Vector2( k.X*2 - 1, k.Y*2 - 1 );
	}

	// Based on iQ https://www.shadertoy.com/view/XdXGW8
	public static float Perlin2D(Vector2 pt)
	{
		// i32eger grid index
		Vector2 i = pt.Floor();
		// Local position in the current grid cell
		Vector2 f = pt.Fract();

		// Smooth out the fractional part to use for blending. This makes the noise gradient muuuuch smoother ( x*x*(3-2*x) ).
		// Check out the graph for y=x*x*(3-2*x) in the range between [0,1], it's an ease-in-out function!
		Vector2 u = new Vector2(
			f.X*f.X*(3 - 2 * f.X),
			f.Y*f.Y*(3 - 2 * f.Y) );

		// Calculate directions for each corner of the cell
		Vector2 tl = HashDir(new Vector2(i.X,   i.Y));
		Vector2 tr = HashDir(new Vector2(i.X+1, i.Y));
		Vector2 bl = HashDir(new Vector2(i.X,   i.Y+1));
		Vector2 br = HashDir(new Vector2(i.X+1, i.Y+1));

		// calculate weights for each corner
		float wtl = Vector2.Dot(tl, f);
		float wtr = Vector2.Dot(tr, new Vector2( f.X-1, f.Y   ));
		float wbl = Vector2.Dot(bl, new Vector2( f.X,   f.Y-1 ));
		float wbr = Vector2.Dot(br, new Vector2( f.X-1, f.Y-1 ));

		// Blend the corners together based on our position within the cell
		// should be the same as Lerp(Lerp(wtl, wtr, u.X), Lerp(wbl, wbr, u.X), u.Y);
		float result = wtl + u.X*(wtr-wtl) + u.Y*(wbl-wtl) + u.X*u.Y*(wtl-wtr-wbl+wbr);
		result *= 2;
		// clamp to [-1,1], as this can occasionally go a little outside
		result = result < -1 ? -1 : (result > 1 ? 1 : result);
		return result;
	}
	public static float Perlin2DN(Vector2 pt) => Perlin2D(pt)*0.5f+0.5f;

	// Based on iQ https://www.shadertoy.com/view/XdXGW8
	public static float Perlin2DTiled(Vector2 pt, int cells)
	{
		pt = pt * cells;

		// i32eger grid index
		Vector2 i = pt.Floor();
		// Local position in the current grid cell
		Vector2 f = pt.Fract();

		// Smooth out the fractional part to use for blending. This makes the noise gradient muuuuch smoother ( x*x*(3-2*x) ).
		// Check out the graph for y=x*x*(3-2*x) in the range between [0,1], it's an ease-in-out function!
		Vector2 u = new Vector2(
			f.X*f.X*(3 - 2 * f.X),
			f.Y*f.Y*(3 - 2 * f.Y) );

		// Calculate directions for each corner of the cell
		Vector2 tl = HashDir(new Vector2( i.X    % cells,  i.Y    % cells));
		Vector2 tr = HashDir(new Vector2((i.X+1) % cells,  i.Y    % cells));
		Vector2 bl = HashDir(new Vector2( i.X    % cells, (i.Y+1) % cells));
		Vector2 br = HashDir(new Vector2((i.X+1) % cells, (i.Y+1) % cells));

		// calculate weights for each corner
		float wtl = Vector2.Dot(tl, f);
		float wtr = Vector2.Dot(tr, new Vector2( f.X-1, f.Y   ));
		float wbl = Vector2.Dot(bl, new Vector2( f.X,   f.Y-1 ));
		float wbr = Vector2.Dot(br, new Vector2( f.X-1, f.Y-1 ));

		// Blend the corners together based on our position within the cell
		// should be the same as Lerp(Lerp(wtl, wtr, u.X), Lerp(wbl, wbr, u.X), u.Y);
		float result = wtl + u.X*(wtr-wtl) + u.Y*(wbl-wtl) + u.X*u.Y*(wtl-wtr-wbl+wbr);
		result *= 2;
		// clamp to [-1,1], as this can occasionally go a little outside
		result = result < -1 ? -1 : (result > 1 ? 1 : result);
		return result;
	}
	public static float Perlin2DTiledN(Vector2 pt, int cells) => Perlin2DTiled(pt, cells)*0.5f + 0.5f;
}