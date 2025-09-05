using StereoKit;
using System;

class WaterSim
{
	public struct Cell
	{
		public float height;
		public float water;
		public Vec2  flow;
	}

	public  Cell[] cells;
	float   cellSize;
	Color[] simData;

	public Tex tex;

	public float gravity  = 9.81f * 10;
	public float rain     = 0;
	public float friction = 0.4f;
	public int   Width  { private set; get; }
	public int   Height { private set; get; }

	public WaterSim(int width, int height, float cellSizeMeters, float[] heights)
	{
		this.Width    = width;
		this.Height   = height;
		this.cellSize = cellSizeMeters;
		cells   = new Cell [width*height];
		simData = new Color[width*height];

		for (int i = 0; i < cells.Length; i++) {
			cells[i].height = heights[i];
			cells[i].water  = 0;
		}

		UpdateData();

		tex = new Tex(TexType.ImageNomips, TexFormat.Rgba128);
		tex.SetColors(width, height, simData);
	}

	public void ResetWater()
	{
		for (int i = 0; i < cells.Length; i++)
			cells[i].water = 0;
	}

	public void AddWater(int x, int y, float amt)
	{
		cells[x + y * Width].water += amt;
	}

	public void Step(float stepTime)
	{
		if (rain > 0)
		{
			float rainAmt = rain * stepTime;
			for (int y = 0; y < Height; y++)
			{
				int ywidth = y * Width;
				for (int x = 1; x < Width; x++)
				{
					int idx = x + ywidth;
					cells[idx].water += rainAmt;
				}
			}
		}

		float frictionFactor = MathF.Pow(1 - friction, stepTime);

		// Gravity for each cell

		float g = gravity * stepTime / cellSize;
		for (int y = 0; y < Height; y++)
		{
			int ywidth = y*Width;
			for (int x = 0; x < Width; x++)
			{
				int idx   = x+ywidth;
				int idx_p = (x == 0 ? Width-1 : x-1)+ywidth;
				cells[idx].flow.x =
					cells[idx].flow.x * frictionFactor + (
						  (cells[idx_p].water + cells[idx_p].height)
						- (cells[idx  ].water + cells[idx  ].height)
					) * g;
			}
		}
		for (int y = 0; y < Height; y++)
		{
			int ywidth   = y*Width;
			int ywidth_p = (y == 0 ? Height-1 : y-1) * Width;
			for (int x = 0; x < Width; x++)
			{
				int idx   = x + ywidth;
				int idx_p = x + ywidth_p;
				cells[idx].flow.y =
					cells[idx].flow.y * frictionFactor + (
					  (cells[idx_p].water + cells[idx_p].height)
					- (cells[idx  ].water + cells[idx  ].height)
					) * g;
			}
		}

		// Cap outflow

		for (int y = 0; y < Height; y++)
		{
			int ywidth   = y * Width;
			int ywidth_n = ((y+1)%Height) * Width;
			for (int x = 0; x < Width; x++)
			{
				int idx    = x + ywidth;
				int idx_nx = ((x+1) % Width) + ywidth;
				int idx_ny = x + ywidth_n;

				float totalOutflow = 0.0f;
				totalOutflow += MathF.Max(0.0f, -cells[idx].flow.x);
				totalOutflow += MathF.Max(0.0f, -cells[idx].flow.y);
				totalOutflow += MathF.Max(0.0f,  cells[idx_nx].flow.x);
				totalOutflow += MathF.Max(0.0f,  cells[idx_ny].flow.y);

				float maxOutflow = cells[idx].water * cellSize * cellSize / stepTime;

				if (totalOutflow > 0.0f)
				{
					float scale = MathF.Min(1.0f, maxOutflow / totalOutflow);

					if (cells[idx   ].flow.x < 0.0f) cells[idx   ].flow.x *= scale;
					if (cells[idx_nx].flow.x > 0.0f) cells[idx_nx].flow.x *= scale;

					if (cells[idx   ].flow.y < 0.0f) cells[idx   ].flow.y *= scale;
					if (cells[idx_ny].flow.y > 0.0f) cells[idx_ny].flow.y *= scale;
				}
			}
		}

		// Make water flow from cell to cell

		for (int y = 0; y < Height; y++)
		{
			int ywidth = y * Width;
			int ywidth_n = ((y + 1) % Height) * Width;
			for (int x = 0; x < Width; x++)
			{
				int idx    = x + ywidth;
				int idx_nx = ((x + 1) % Width) + ywidth;
				int idx_ny = x + ywidth_n;

				cells[idx].water += (
						  cells[idx   ].flow.x + cells[idx   ].flow.y
						- cells[idx_nx].flow.x - cells[idx_ny].flow.y
					) * stepTime / cellSize / cellSize;
			}
		}

		UpdateData();
		tex.SetColors(Width, Height, simData);
	}

	void UpdateData()
	{
		for (int y = 0; y < Height; y++)
		{
			int ywidth = y * Width;
			for (int x = 0; x < Width; x++)
			{
				int idx = x + ywidth;

				float waterPct  = MathF.Min(1, cells[idx].water  / 100);
				float heightPct = MathF.Min(1, cells[idx].height / 100);
				simData[idx] = new Color(heightPct, waterPct, 0, 0);
			}
		}
	}
}
