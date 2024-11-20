using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HumanPlayer : MonoBehaviour, PlayerInterface {

	bool skip=false;
	bool drew =false;
	bool playedWild;
	string name;
	List<Card> handList = new List<Card> ();

	public HumanPlayer(string name) { //initalizes
		this.name = name;
	}

	public bool skipStatus { //returns if the player should be skipped
		get{return skip; }
		set{ skip = value; }
	}

	public void turn() { //does the turn
		playedWild = false;
		drew = false;
		int i = 0;
		foreach (Card x in handList) { //foreach card in hand
			
			GameObject temp = null;
			if (GameObject.Find ("Control").GetComponent<Control> ().playerHand.transform.childCount > i) //is the card already there or does it need to be loaded
				temp = GameObject.Find ("Control").GetComponent<Control> ().playerHand.transform.GetChild (i).gameObject;			
			else 
				temp = x.loadCard (GameObject.Find ("Control").GetComponent<Control> ().playerHand.transform);

			
			if (handList [i].Equals (Control.discard [Control.discard.Count - 1]) || handList [i].getNumb () >= 13) { //if the cards can be played
				setListeners (i, temp);
			}
			else {
				temp.transform.GetChild (3).gameObject.SetActive (true); //otherwise black them out
			}
			i++;
		}
	}
	public void setListeners(int where,GameObject temp) { //sets all listeners on the cards
		temp.GetComponent<Button> ().onClick.AddListener (() => {
			playedWild = handList[where].getNumb()>=13;

			temp.GetComponent<Button>().onClick.RemoveAllListeners();
			Destroy (temp);
			turnEnd(where);
		});
	}
	public void addCards(Card other) { //recieves cards to add to the hand
		handList.Add (other);
	}
	public void recieveDrawOnTurn() { //if the player decides to draw
		handList[handList.Count-1].loadCard (GameObject.Find ("Control").GetComponent<Control> ().playerHand.transform);
		drew = true;
		turnEnd (-1);
	}
	public void turnEnd(int where)
	{
		Control cont = GameObject.Find("Control").GetComponent<Control>();
		cont.playerHand.GetComponent<RectTransform>().localPosition = new Vector2(0, 0);

		// Clear button listeners and reset hand UI
		for (int i = cont.playerHand.transform.childCount - 1; i >= 0; i--)
		{
			cont.playerHand.transform.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
			cont.playerHand.transform.GetChild(i).GetChild(3).gameObject.SetActive(false);
		}

		if (drew)
		{
			cont.GetComponent<Control>().enabled = true;
			cont.recieveText($"{name} drew a card");
			cont.deckGO.GetComponent<Button>().onClick.RemoveAllListeners();
			return;
		}

		Card playedCard = handList[where]; // Save reference before modifying handList
		int specNumb = playedCard.getNumb();

		if (playedWild)
		{
			cont.updateDiscPile(playedCard);
			handList.RemoveAt(where);
			cont.startWild(name);
			if (specNumb == 14)
				cont.specialCardPlay(this, 14);
		}
		else
		{
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
			cont.updateDiscPile(playedCard);
			handList.RemoveAt(where);
		}

		// Play audio based on the saved card details
		int toneIndex = playedCard.getNumb();
		string color = playedCard.getColor();
		if (toneIndex >= 0 && toneIndex < 10)
		{
			cont.PlayCardAudio(color, toneIndex);
		}
	}
	public bool Equals(PlayerInterface other) { //equals function based on name
		return other.getName ().Equals (name);
	}
	public string getName() { //returns the name
		return name;
	}
	public int getCardsLeft() { //gets how many cards are left in the hand
		return handList.Count;
	}
}
