using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerResort.Entities.Lobby;

namespace TowerResort.GameComponents;

public class TriviaMania : EntityComponent<ModelEntity>, ISingletonComponent
{
	public enum GameStatus
	{
		Idle,
		Starting,
		Active,
		Pause,
	}
	public GameStatus CurGameStatus { get; private set; }

	public List<Question> PastQuestion;

	public List<LobbyPawn> Players;

	Question curQuestion;

	public void StartTrivia()
	{
		CurGameStatus = GameStatus.Active;
		PastQuestion = Questions.OrderBy( x => Game.Random.Int( 1, 1000 ) ).ToList();

		NextQuestion();
	}

	protected override void OnDeactivate()
	{
		KickPlayers();

		base.OnDeactivate();
	}

	public void JoinGame( Entity entity )
	{
		var joiner = entity as LobbyPawn;
		if ( joiner == null ) return;

		joiner.FreezeMovement = MainPawn.FreezeEnum.Movement;
		joiner.FocusedEntity = Entity;

		Players.Add( joiner );
	}

	public void LeaveGame( LobbyPawn leaver )
	{
		//DisplayMessage( leaver, "You have left the table", 5.0f );
		leaver.FreezeMovement = MainPawn.FreezeEnum.None;
		leaver.FocusedEntity = null;

		Players.Remove( leaver );
	}

	public bool IsCorrect()
	{
		return true;
	}

	public void NextQuestion()
	{
		curQuestion = PastQuestion[Game.Random.Int( 0, Questions.Length )];

		var answers = curQuestion.PossibleAnswers;

		Log.Info( $"Question: {curQuestion.DisplayQuestion}" +
			$"\n1 - {answers.AnswerOne}" +
			$"\n2 - {answers.AnswerTwo}" +
			$"\n2 - {answers.AnswerThree}" +
			$"\n4 - {answers.AnswerFour}" );
	}

	public void KickPlayers()
	{
		foreach ( LobbyPawn player in Players.ToArray() )
			LeaveGame( player );
	}

	protected override void OnActivate()
	{
		Players = new List<LobbyPawn>();
		CurGameStatus = GameStatus.Idle;

		base.OnActivate();
	}

	public static Question[] Questions => new Question[]
	{
		new Question("What is the meaning of life", new string[] {"Die", "Love", "What?", "S&Box oh S&Box" }, 4),
		new Question("When did Garry's Mod release", new string[] { "2004", "2007", "2006", "2008" }, 3 ),
		//new Question("")
	};

	TimeUntil timeBeforeStart;
	const float timeToStart = 15.0f;

	[Event.Tick.Server]
	public void TickGame()
	{
		foreach ( LobbyPawn player in Players.ToArray() )
		{
			if ( player.Position.Distance( Entity.Position ) > 92.0f )
				LeaveGame( player );
		}

		if (CurGameStatus == GameStatus.Idle)
		{
			if ( Players.Count() >= 1 )
			{
				timeBeforeStart = timeToStart;
				CurGameStatus = GameStatus.Starting;
			}
		}
			
		if ( CurGameStatus == GameStatus.Starting )
		{
			if(Players.Count() < 1)
				CurGameStatus = GameStatus.Idle;
			else if ( timeBeforeStart <= 0.0f )
				StartTrivia();
		}
	}
}

public class Answers
{
	public string AnswerOne;
	public string AnswerTwo;
	public string AnswerThree;
	public string AnswerFour;

	public int CorrectAnswer = -1;

	public Answers()
	{

	}

	public Answers(string[] answers, int correct)
	{
		AnswerOne = answers[0];
		AnswerTwo = answers[1];
		AnswerThree = answers[2];
		AnswerFour = answers[3];

		CorrectAnswer = correct-1;
	}
}

public class Question
{
	public bool TrueOrFalse = false;
	public string DisplayQuestion { get; set; }
	public Answers PossibleAnswers { get; set; }

	public Question()
	{

	}

	public Question(string display, string[] answers, int correctIndex, bool trueOrFalse = false)
	{
		DisplayQuestion = display;
		PossibleAnswers = new Answers( answers, correctIndex );
		TrueOrFalse = trueOrFalse;
	}
}
