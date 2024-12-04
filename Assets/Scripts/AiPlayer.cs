using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class AiPlayer : MonoBehaviour, PlayerInterface {

	bool skip=false;
	bool drew =false;
	string name;
	string colorToPlay;
	int locationCardPlayed=-1;
	List<Card> handList = new List<Card> ();

	public AiPlayer(string name) { //initializes
		this.name = name;
	}

	public bool skipStatus {
		get{return skip; }
		set{ skip = value; }
	}

	public void turn() { //the ai's turn
		Control control = GameObject.Find("Control").GetComponent<Control>();
		control.StartCoroutine(AITurnRoutine());
	}

	private IEnumerator AITurnRoutine()
	{
		// Wait for the configured AI wait time
		float aiWaitTime = GameObject.Find("Control").GetComponent<Control>().aiWaitTime;
		Control control = GameObject.Find("Control").GetComponent<Control>();
		
		double startDSPTime = AudioSettings.dspTime;
		while (AudioSettings.dspTime - startDSPTime < aiWaitTime)
		{
			control.audioTimer = (float) (AudioSettings.dspTime - control.globalDSPStartTime) % control.audioLength;
			yield return null;
		}

		// Execute the AI's turn logic
		Card currDisc = Control.discard[Control.discard.Count - 1];
		string colorDisc = currDisc.getColor();
		int numbDisc = currDisc.getNumb();
		if (numbDisc == 13 || numbDisc == 14)
			numbDisc = -1;

		locationCardPlayed = -1;
		colorToPlay = "";
		drew = false;

		handList = handList.OrderBy(e => e.getColor()).ThenBy(e => e.getNumb()).ToList();

		int count = handList.Count(e => e.getColor() == colorDisc);
		int count2 = handList.Count(e => e.getNumb() == numbDisc);
		int count3 = handList.Count(e => e.getNumb() == 13 || e.getNumb() == 14);

		if (count > 0)
			locationCardPlayed = handList.FindLastIndex(e => e.getColor() == colorDisc);
		else if (count2 > 0)
			locationCardPlayed = handList.FindIndex(e => e.getNumb() == numbDisc);
		else if (count3 > 0)
		{
			locationCardPlayed = handList.FindIndex(e => e.getNumb() == 13 || e.getNumb() == 14);
			colorToPlay = handList.Max(e => e.getColor());
			if (colorToPlay.Equals("Black"))
			{
				bool first = true;
				while (colorToPlay.Equals(colorDisc) || first)
				{
					int rand = Random.Range(1, 4);
					switch (rand)
					{
						case 1: colorToPlay = "Red"; break;
						case 2: colorToPlay = "Blue"; break;
						case 3: colorToPlay = "Green"; break;
						case 4: colorToPlay = "Yellow"; break;
					}
					first = false;
				}
			}
		}
		else
		{
			GameObject.Find("Control").GetComponent<Control>().draw(1, this);
			GameObject.Find("Control").GetComponent<Control>().PlayDrawSound();
			drew = true;
		}

		if (locationCardPlayed == -1 && !drew)
			yield break; // End coroutine if no valid action is found

		turnEnd(); // Complete the turn
	}

	public void addCards(Card other) { //recieves cards to the hand
		handList.Add (other);
	}

	public void turnEnd()
	{
		Control cont = GameObject.Find("Control").GetComponent<Control>();
		cont.ResetEffects();

		if (drew)
		{
			cont.recieveText($"{name} drew");
			cont.enabled = true;
			return;
		}

		// Save card data before modifying the hand list
		Card playedCard = handList[locationCardPlayed];
		int toneIndex = playedCard.getNumb();
		string color = playedCard.getColor();

		// Handle special card effects and update discard pile
		int specNumb = playedCard.getNumb();
		if (specNumb < 10)
		{
			cont.recieveText($"{name} played a {playedCard.getColor()} {playedCard.getNumb()}");
			cont.enabled = true;
		}
		else if (specNumb == 10)
		{
			cont.specialCardPlay(this, 10);
			cont.recieveText($"{name} played a {playedCard.getColor()} skip");
		}
		else if (specNumb == 11)
		{
			cont.specialCardPlay(this, 11);
			cont.recieveText($"{name} played a {playedCard.getColor()} reverse");
		}
		else if (specNumb == 12)
		{
			cont.specialCardPlay(this, 12);
			cont.recieveText($"{name} played a {playedCard.getColor()} draw 2");
		}
		else if (specNumb == 13)
		{
			cont.specialCardPlay(this, 13);
			cont.recieveText($"{name} played a wild card, Color: {colorToPlay}");
			playedCard.changeColor(colorToPlay);
			cont.enabled = true;
		}
		else if (specNumb == 14)
		{
			cont.specialCardPlay(this, 14);
			cont.recieveText($"{name} played a wild draw 4, Color: {colorToPlay}");
			playedCard.changeColor(colorToPlay);
			cont.enabled = true;
		}

		// Play card audio using saved data
		if (toneIndex >= 0 && toneIndex < 10)
		{
			//cont.StartAudioTimeline(null);
			cont.PlayCardAudio(color, toneIndex);
			cont.SyncAllAudioSources();
		}

		// Update discard pile and remove the card
		cont.updateDiscPile(playedCard);
		handList.RemoveAt(locationCardPlayed);
	}

	public bool Equals(PlayerInterface other) { //equals
		return other.getName ().Equals (name);
	}
	public string getName() { //returns the name
		return name;
	}
	public int getCardsLeft() { //returns cards left
		return handList.Count;
	}
}
