using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.GameComponents;

public partial class TetrisGame : EntityComponent, ISingletonComponent
{
	TetrisBlock curBlock;
	public LobbyPawn Player { get; protected set; }

	public TetrisBlock CurBlock 
	{
		get => curBlock;
		set
		{
			curBlock = value;
			CurBlock.Reset();

			for ( int i = 0; i < 2; i++ )
			{
				curBlock.Move( 1, 0 );

				if ( !BlockFits() )
					curBlock.Move( -1, 0 );
			}
		}
	}

	TetrisUI tetrisPanel;

	public TetrisGrid Grid { get; set; }

	public TetrisBlockQueue BlockQueue;
	public bool GameOver { get; private set; }

	protected override void OnActivate()
	{
		base.OnActivate();
		BlockQueue = new TetrisBlockQueue();
	}

	protected override void OnDeactivate()
	{
		if ( Player == null ) return;

		Player.FocusedEntity = null;
		Player.FreezeMovement = MainPawn.FreezeEnum.None;
	}

	bool BlockFits()
	{
		foreach ( TetrisPosition p in CurBlock.TilePositions() )
		{
			if ( !Grid.IsEmptyCell( p.Row, p.Column ) )
				return false;
		}

		return true;
	}

	public void SimulateTetris(IClient cl)
	{
		if ( CurBlock == null )
		{
			CurBlock = BlockQueue.GetAndUpdate();
			CurBlock.Reset();
		}

		if ( Input.AnalogMove.y == 1 )
			MoveBlockLeft();

		if ( Input.AnalogMove.y == -1 )
			MoveBlockRight();

		if ( Input.AnalogMove.x == -1 )
			MoveBlockDown();

		if ( Input.Pressed( InputButton.Jump ) )
			DropBlock();

		if ( Input.Pressed( InputButton.Use ) )
			RotateBlockCW();

		if ( Input.Pressed( InputButton.Menu ) )
			RotateBlockCCW();

		if ( Input.Pressed( InputButton.PrimaryAttack ) )
			StopGame();
	}

	public void StartGame(LobbyPawn newPlayer)
	{
		GameOver = false;

		Grid = new TetrisGrid( 22, 10 );

		Player = newPlayer;
		Player.FreezeMovement = MainPawn.FreezeEnum.Movement;

		StartGameClient( To.Single( Player ) );

		_ = GameLoop();
	}

	public void StopGame()
	{
		GameOver = true;

		StopGameClient( To.Single( Player ) );

		curBlock = null;
		Grid.ResetGrid();

		Player.FocusedEntity = null;
		Player.FreezeMovement = MainPawn.FreezeEnum.None;
		Player = null;
	}

	[ClientRpc]
	public void StartGameClient()
	{
		tetrisPanel = new TetrisUI();
	}

	[ClientRpc]
	public void StopGameClient()
	{
		tetrisPanel?.Delete();
		tetrisPanel = null;
	}
	public void RotateBlockCW()
	{
		CurBlock.RotateCW();

		if ( !BlockFits() )
			CurBlock.RotateCCW();
	}

	public void RotateBlockCCW()
	{
		CurBlock.RotateCCW();

		if ( !BlockFits() )
			CurBlock.RotateCW();
	}

	public void MoveBlockLeft()
	{
		CurBlock.Move( 0, -1 );

		if ( !BlockFits() )
			CurBlock.Move( 0, 1 );
	}

	public void MoveBlockRight()
	{
		CurBlock.Move( 0, 1 );

		if ( !BlockFits() )
			CurBlock.Move( 0, -1 );
	}

	public void MoveBlockDown()
	{
		CurBlock.Move( 1, 0 );

		if ( !BlockFits() )
		{
			CurBlock.Move( -1, 0 );
			PlaceBlock();
		}
	}

	int TileDropDistance(TetrisPosition p)
	{
		int drop = 0;

		while ( Grid.IsEmptyCell( p.Row + drop + 1, p.Column ) )
			drop++;

		return drop;
	}

	public int BlockDropDistance()
	{
		int drop = Grid.Rows;

		foreach ( TetrisPosition p in CurBlock.TilePositions() )
			drop = Math.Min( drop, TileDropDistance( p ) );

		return drop;
	}

	public void DropBlock()
	{
		CurBlock.Move( BlockDropDistance(), 0 );
		PlaceBlock();
	}

	TetrisBlock ghostBlock;

	void DrawGhostBlock(TetrisBlock block)
	{
		int dropDist = BlockDropDistance();

		ghostBlock = block;

		foreach ( TetrisPosition p in block.TilePositions())
		{

		}
	}

	async Task GameLoop()
	{
		while ( !GameOver )
		{
			await Task.Delay( 500 );
			MoveBlockDown();
		}
	}

	bool IsGameOver()
	{
		return !(Grid.IsRowCellEmpty( 0 ) && Grid.IsRowCellEmpty( 1 ));
	}

	void PlaceBlock()
	{
		foreach ( TetrisPosition p in CurBlock.TilePositions() )
			Grid.SetGridID( p.Row, p.Column, CurBlock.Id );

		Grid.ClearFullRows();

		if ( IsGameOver() )
			StopGame();
		else
			CurBlock = BlockQueue.GetAndUpdate();
	}
}

public abstract class TetrisBlock
{
	protected abstract TetrisPosition[][] Tiles { get; }
	protected abstract TetrisPosition StartOffset { get; }
	public abstract int Id { get; }

	int rotationSprite;
	TetrisPosition offset;

	public TetrisBlock()
	{
		offset = new TetrisPosition( StartOffset.Row, StartOffset.Column );
	}

	public IEnumerable<TetrisPosition> TilePositions()
	{
		foreach ( TetrisPosition p in Tiles[rotationSprite] )
		{
			yield return new TetrisPosition( p.Row + offset.Row, p.Column + offset.Column );
		}
	}

	public int GetRow()
	{
		return offset.Row;
	}

	public int GetColumn()
	{
		return offset.Column;
	}

	public void RotateCW()
	{
		rotationSprite = (rotationSprite + 1) % Tiles.Length;
	}

	public void RotateCCW()
	{
		if ( rotationSprite == 0 )
			rotationSprite = Tiles.Length - 1;
		else
			rotationSprite -= 1;
	}

	public void Move(int rows, int columns)
	{
		offset.Row += rows;
		offset.Column += columns;
	}

	public void Reset()
	{
		rotationSprite = 0;
		offset.Row = StartOffset.Row;
		offset.Column = StartOffset.Column;
	}
}

public class TetrisPosition
{
	public int Row { get; set; }
	public int Column { get; set; }

	public TetrisPosition(int row, int column)
	{
		Row = row;
		Column = column;
	}
}

public class TetrisGrid
{
	readonly int[,] grid;
	public int Rows { get; }
	public int Columns { get; }

	public TetrisGrid( int rows, int columns )
	{
		Rows = rows;
		Columns = columns;
		grid = new int[Rows, Columns];
	}

	public void SetGridID(int row, int col, int id)
	{
		grid[row, col] = id;
	}

	public void SetGrid( int row, int col )
	{
		grid.SetValue( row, col );
	}

	public object GetGrid()
	{
		return grid.GetValue( Rows, Columns );
	}

	public bool IsInsideCell( int r, int c )
	{
		return r >= 0 && r < Rows && c >= Columns && c < Columns;
	}

	public bool IsEmptyCell( int r, int c )
	{
		return IsInsideCell( r, c ) && grid[r, c] == 0;
	}

	public bool IsRowCellFull( int r )
	{
		for ( int c = 0; c < Columns; c++ )
		{
			if ( grid[r, c] == 0 )
			{
				return false;
			}
		}

		return true;
	}

	public bool IsRowCellEmpty( int r )
	{
		for ( int c = 0; c < Columns; c++ )
		{
			if ( grid[r, c] != 0 )
			{
				return false;
			}
		}

		return true;
	}

	public void ClearRow( int r )
	{
		for ( int c = 0; c < Columns; c++ )
		{
			grid[r, c] = 0;
		}
	}

	public void ResetGrid()
	{
		for ( int r = 0; r < Rows; r++ )
		{
			for ( int c = 0; c < Columns; c++ )
			{
				grid[r, c] = 0;
			}
		}
	}

	public void MoveRowDown( int r, int numRows )
	{
		for ( int c = 0; c < Columns; c++ )
		{
			grid[r + numRows, c] = grid[r, c];
			grid[r, c] = 0;
		}
	}

	public int ClearFullRows()
	{
		int cleared = 0;

		for ( int r = Rows - 1; r >= 0; r-- )
		{
			if ( IsRowCellFull( r ) )
			{
				ClearRow( r );
				cleared++;
			}
			else if ( cleared > 0 )
				MoveRowDown( r, cleared );
		}

		return cleared;
	}
}

public class TetrisBlockQueue
{
	readonly TetrisBlock[] blocks = new TetrisBlock[]
	{
		new IBlock(),
		new JBlock(),
		new LBlock(),
		new OBlock(),
		new SBlock(),
		new TBlock(),
		new ZBlock()
	};

	readonly Random random = new Random();

	public TetrisBlock NextBlock { get; private set; }

	public TetrisBlockQueue()
	{
		NextBlock = RandomBlock();
	}

	public TetrisBlock RandomBlock()
	{
		return blocks[random.Next( blocks.Length )];
	}

	public TetrisBlock GetAndUpdate()
	{
		TetrisBlock block = NextBlock;

		do
		{
			NextBlock = RandomBlock();
		}
		while( block.Id == NextBlock.Id);

		return block;
	}
}

public class OBlock : TetrisBlock
{
	readonly TetrisPosition[][] tiles = new TetrisPosition[][]
	{
		new TetrisPosition[] { new(0,0), new(0,1), new (1,0), new(1,0)},
	};

	public override int Id => 4;
	protected override TetrisPosition StartOffset => new TetrisPosition( 0, 4 );
	protected override TetrisPosition[][] Tiles => tiles;
}

public class ZBlock : TetrisBlock
{
	readonly TetrisPosition[][] tiles = new TetrisPosition[][]
	{
		new TetrisPosition[] { new(0,0), new(0,1), new (1,1), new(1,2)},
		new TetrisPosition[] { new(0,2), new(1,1), new (1,2), new(2,1)},
		new TetrisPosition[] { new(1,0), new(1,1), new (2,1), new(2,2)},
		new TetrisPosition[] { new(0,1), new(1,0), new (1,1), new(2,0)}
	};

	public override int Id => 7;
	protected override TetrisPosition StartOffset => new TetrisPosition( 0, 3 );
	protected override TetrisPosition[][] Tiles => tiles;
}

public class TBlock : TetrisBlock
{
	readonly TetrisPosition[][] tiles = new TetrisPosition[][]
	{
		new TetrisPosition[] { new(0,1), new(1,0), new (1,1), new(1,2)},
		new TetrisPosition[] { new(0,1), new(1,1), new (1,2), new(2,1)},
		new TetrisPosition[] { new(1,0), new(1,1), new (1,2), new(2,1)},
		new TetrisPosition[] { new(0,1), new(1,0), new (1,1), new(2,1)}
	};

	public override int Id => 6;
	protected override TetrisPosition StartOffset => new TetrisPosition( 0, 3 );
	protected override TetrisPosition[][] Tiles => tiles;
}

public class SBlock : TetrisBlock
{
	readonly TetrisPosition[][] tiles = new TetrisPosition[][]
	{
		new TetrisPosition[] { new(0,1), new(0,2), new (1,0), new(1,1)},
		new TetrisPosition[] { new(0,1), new(1,1), new (1,2), new(2,2)},
		new TetrisPosition[] { new(1,1), new(1,2), new (2,0), new(2,1)},
		new TetrisPosition[] { new(0,0), new(1,0), new (1,1), new(2,1)}
	};

	public override int Id => 5;
	protected override TetrisPosition StartOffset => new TetrisPosition( 0, 3 );
	protected override TetrisPosition[][] Tiles => tiles;
}

public class LBlock : TetrisBlock
{
	readonly TetrisPosition[][] tiles = new TetrisPosition[][]
	{
		new TetrisPosition[] { new(0,2), new(1,0), new (1,1), new(1,2)},
		new TetrisPosition[] { new(0,1), new(1,1), new (2,1), new(2,2)},
		new TetrisPosition[] { new(1,0), new(1,1), new (1,2), new(2,0)},
		new TetrisPosition[] { new(0,0), new(0,1), new (1,1), new(2,1)}
	};

	public override int Id => 3;
	protected override TetrisPosition StartOffset => new TetrisPosition( 0, 3 );
	protected override TetrisPosition[][] Tiles => tiles;
}

public class JBlock : TetrisBlock
{
	readonly TetrisPosition[][] tiles = new TetrisPosition[][]
	{
		new TetrisPosition[] { new(0,0), new(1,0), new (1,1), new(1,2)},
		new TetrisPosition[] { new(0,1), new(0,2), new (1,1), new(2,1)},
		new TetrisPosition[] { new(1,0), new(1,1), new (1,2), new(2,2)},
		new TetrisPosition[] { new(0,1), new(1,1), new (2,0), new(2,1)}
	};

	public override int Id => 2;
	protected override TetrisPosition StartOffset => new TetrisPosition( 0, 3 );
	protected override TetrisPosition[][] Tiles => tiles;
}

public class IBlock : TetrisBlock
{
	readonly TetrisPosition[][] tiles = new TetrisPosition[][]
	{
		new TetrisPosition[] { new(1,0), new(1,1), new (1,2), new(1,3)},
		new TetrisPosition[] { new(0,2), new(1,2), new (2,2), new(3,2)},
		new TetrisPosition[] { new(2,0), new(2,1), new (2,2), new(2,3)},
		new TetrisPosition[] { new(0,1), new(1,1), new (2,1), new(3,1)}
	};

	public override int Id => 1;
	protected override TetrisPosition StartOffset => new TetrisPosition( -1, 3 );
	protected override TetrisPosition[][] Tiles => tiles;
}
