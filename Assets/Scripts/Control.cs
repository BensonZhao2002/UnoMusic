using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Audio;

public class Control : MonoBehaviour {

	List<PlayerInterface> players = new List<PlayerInterface>();
	public static List<Card> deck = new List<Card>();
	public static List<Card> discard = new List<Card>();
	public GameObject playerHand;
	public GameObject contentHolder;
	public Text dialogueText;

	public static GameObject discardPileObj;

	public GameObject regCardPrefab;
	public GameObject skipCardPrefab;
	public GameObject revrsCardPrefab;
	public GameObject drawCardPrefab;
	public GameObject wildCardPrefab;

	public GameObject[] colors = new GameObject[4];
	string[] colorsMatch = new string[4]{"Yellow","Green","Blue","Red"};
	public GameObject[] aiPlayers = new GameObject[5];
	public GameObject colorText;
	public GameObject deckGO;
	public GameObject pauseCan;
	public GameObject endCan;
	bool enabledStat=false;

	int where=0;
	float timer=0;
	bool reverse=false;

	public static int numbOfAI;

	public AudioSource melodySource;
	public AudioSource chordSource;
	public AudioSource bassSource;
	public AudioSource drumSource;

	public float audioTimer = 0f; // Tracks elapsed time since the first audio started
	private bool timerStarted = false; // Ensures timer starts with the first audio
	public float audioLength = 0f; // Automatically determined audio length
	public float originalAudioLength = 0f;
	public double globalDSPStartTime = 0;

	private bool waitingForWildColor = false;

	private Dictionary<string, string> effectToInstrument = new Dictionary<string, string>()
	{
		{"Reverb", null},
		{"Chorus", null},
		{"Flanger", null}
	};

	private Dictionary<string, int> lastPlayedCardToneIndex = new Dictionary<string, int>() {
		{"Red", -1},
		{"Green", -1},
		{"Blue", -1},
		{"Yellow", -1}
	};

	private Dictionary<string, string> colorToInstrument = new Dictionary<string, string>
	{
		{ "Red", "Melody" },
		{ "Green", "Chord" },
		{ "Blue", "Bass" },
		{ "Yellow", "Drum" }
	};

	private Dictionary<string, int> audioGroupIndices = new Dictionary<string, int>();

	private bool isMajorScale = true;

	public float aiWaitTime = 2.0f;

	void Start () { //this does all the setup. Makes the human and ai players. sets the deck and gets the game ready
		InitializeRandomAudioIndices();
		discard.Clear ();
		deck.Clear ();

		players.Add (new HumanPlayer ("You"));
		for (int i = 0; i < numbOfAI; i++) {
			players.Add (new AiPlayer ("Player "+(i+1)));
		}

		for (int i = 0; i < players.Count - 1; i++) {
			aiPlayers [i].SetActive (true);
			aiPlayers [i].transform.Find ("Name").GetComponent<Text> ().text = players [i + 1].getName ();
		}

		for (int i = 0; i < 15; i++) { //setups the deck by making cards
			for (int j = 0; j < 8; j++) {
				switch (i) {
					case 10:
						deck.Add (new Card (i, returnColorName (j%4), skipCardPrefab));
						break;
					case 11:
						deck.Add (new Card (i, returnColorName (j%4), revrsCardPrefab));
						break;
					case 12:
						deck.Add (new Card (i, returnColorName (j%4), drawCardPrefab));
						break;
					case 13:
						deck.Add (new Card (i, "Black", wildCardPrefab));
						break;
					case 14:
						deck.Add (new Card (i, "Black", wildCardPrefab));
						break;
					default:
						deck.Add (new Card (i, returnColorName (j%4), regCardPrefab));
						break;						
				}

				if ((i == 0 || i>=13) && j >= 3)
					break;
			}
		}
		shuffle ();

		Card first = null;
		if (deck [0].getNumb () < 10) {
			first = deck [0];
		}
		else {
			while (deck [0].getNumb () >= 10) {
				deck.Add (deck [0]);
				deck.RemoveAt (0);
			}
			first = deck [0];
		}
		discard.Add (first);
		discardPileObj = first.loadCard (725, -325, GameObject.Find ("Main").transform);
		deck.RemoveAt (0);

		foreach (PlayerInterface x in players) {
			for (int i = 0; i < 7; i++) {
				x.addCards (deck [0]);
				deck.RemoveAt (0);
			}
		}
	}

	public void StartAudioTimeline(AudioClip firstClip)
	{
		if (!timerStarted)
		{
			if (firstClip == null)
			{
				return;
			}
			timerStarted = true;
			globalDSPStartTime = AudioSettings.dspTime;

			// Set the audio length based on the first clip
			if (firstClip != null)
			{
				audioLength = firstClip.length;
				originalAudioLength = firstClip.length;
				//Debug.Log($"Audio length set to: {audioLength} seconds");
			}
			else if (firstClip != null && audioLength == 0f)
			{
				audioLength = firstClip.length;
				originalAudioLength = firstClip.length;
			}
		}
	}

	string returnColorName (int numb) { //returns a color based on a number, used in setup
		switch(numb) {
		case 0: 
			return "Green";
		case 1:
			return "Blue";
		case 2: 
			return "Red";
		case 3: 
			return "Yellow";
		}
		return "";
	}
	void shuffle() { //shuffles the deck by changing cards around
		for (int i = 0; i < deck.Count; i++) {
			Card temp = deck.ElementAt (i);
			int posSwitch = Random.Range (0, deck.Count);
			deck [i] = deck [posSwitch];
			deck [posSwitch] = temp;
		}
	}
	public void recieveText(string text) { //updates the dialogue box
		dialogueText.text += text + "\n";
		contentHolder.GetComponent<RectTransform> ().localPosition = new Vector2 (0, contentHolder.GetComponent<RectTransform> ().sizeDelta.y);
	}
	public void updateDiscPile(Card card) { //this changes the last card played. Top of the discard pile
		discard.Add (card);
		Destroy(discardPileObj);
		discardPileObj=card.loadCard (725, -325, GameObject.Find ("Main").transform);
		discardPileObj.transform.SetSiblingIndex(9);
	}
	public bool updateCardsLeft() { //this updates the number below each ai, so the player knows how many cards they have left
		for (int i = 0; i < players.Count - 1; i++) {
			int temp = players [i + 1].getCardsLeft ();
			aiPlayers [i].transform.Find ("CardsLeft").GetComponent<Text> ().text = temp.ToString();
		}
		foreach (PlayerInterface i in players) {
			if (i.getCardsLeft()==0) {
				this.enabled = false;
				recieveText (string.Format ("{0} won!", i.getName()));
				endCan.SetActive (true);
				endCan.transform.Find ("WinnerTxt").gameObject.GetComponent<Text> ().text = string.Format ("{0} Won!", i.getName ());
				return true;
			}
		}
		return false;
	}

	void Update() { //this runs the players turns
		if (waitingForWildColor) // Pause the game if waiting for Wild Card color selection
			return;

		bool win = updateCardsLeft();
		if (win) {
			SetMasterPitch(1f);
			return;
		}
		if (timerStarted)
		{
			audioTimer = (float)((AudioSettings.dspTime - globalDSPStartTime)) % audioLength;
			audioTimer = Mathf.Clamp(audioTimer, 0, audioLength - 0.01f);
		}

		bool shouldRaisePitch = players.Any(player => player.getCardsLeft() < 3);
		if (shouldRaisePitch)
		{
			SetMasterPitch(1.1667f); // Raise the pitch
		}
		else {
			SetMasterPitch(1f); // Reset to normal pitch
		}

		if (players [where] is HumanPlayer) {
			if (players [where].skipStatus) {
				players [where].skipStatus = false;
				where += reverse ? -1 : 1;
				if (where >= players.Count)
					where = 0;
				else if (where < 0)
					where = players.Count - 1;
				return;
			}
			this.enabled = false;
			PlayerInterface temp = players [where];
			deckGO.GetComponent<Button> ().onClick.RemoveAllListeners ();
			deckGO.GetComponent<Button> ().onClick.AddListener (() => {
				draw (1, temp);
				PlayDrawSound();
				((HumanPlayer)temp).recieveDrawOnTurn();
			});
			where+=reverse?-1:1;
			players [where+(reverse?1:-1)].turn ();
		}
		else if (players [where] != null) {
			if (players [where].skipStatus) {
				players [where].skipStatus = false;
				where += reverse ? -1 : 1;
				if (where >= players.Count)
					where = 0;
				else if (where < 0)
					where = players.Count - 1;
				return;
			}
			timer += Time.deltaTime;
			if (timer < 2.2)
				return;
			this.enabled = false;
			timer = 0;
			where+=reverse?-1:1;
			players [where+(reverse?1:-1)].turn ();
		}
		else
			where += reverse ? -1 : 1;
	
		if (where >= players.Count)
			where = 0;
		else if (where < 0)
			where = players.Count - 1;
			
	}

	public void startWild(string name) { //this starts the color chooser for the player to choose a color after playing a  wild
		waitingForWildColor = true;
		for (int i = 0; i < 4; i++) {
			colors [i].SetActive (true);
			addWildListeners (i, name);
		}
		colorText.SetActive (true);
	}

	public void addWildListeners(int i, string name) { //this is ran from the start wild. It sets each color option as a button and sets the onclick events
		colors [i].GetComponent<Button> ().onClick.AddListener (() => {
			discard[discard.Count-1].changeColor(colorsMatch[i]);
			recieveText(string.Format("{0} played a wild, Color: {1}",name,colorsMatch[i]));

			Destroy(discardPileObj);
			discardPileObj=discard[discard.Count-1].loadCard (725, -325, GameObject.Find ("Main").transform);
			discardPileObj.transform.SetSiblingIndex(9);
			 
			foreach (GameObject x in colors) {
				x.SetActive (false);
				x.GetComponent<Button>().onClick.RemoveAllListeners();
			}
			colorText.SetActive (false);
			waitingForWildColor = false;
			this.enabled=true;
		});
	}

	public void draw(int amount, PlayerInterface who) { //gives cards to the players. Players can ask to draw or draw will actrivate from special cards
		if (deck.Count < amount) {
			resetDeck ();
		}
		for (int i = 0; i < amount; i++) {
			who.addCards (deck [0]);
			deck.RemoveAt (0);
		}
	}

	public void resetDeck() { //this resets the deck when all of the cards run out
		print ("reseting");
		foreach (Card x in discard) {
			if (x.getNumb () == 13 || x.getNumb () == 14) {
				x.changeColor ("Black");
			}
			deck.Add (x);
		}
		shuffle ();
		Card last = discard [discard.Count - 1];
		discard.Clear ();
		discard.Add (last);
	}

	public void specialCardPlay(PlayerInterface player, int cardNumb) { //takes care of all special cards played
		int who = players.FindIndex (e=>e.Equals(player)) + (reverse?-1:1);
		if (who >= players.Count)
			who = 0;
		else if (who < 0)
			who = players.Count - 1;

		string cardColor = Control.discard[Control.discard.Count - 1].getColor();
		switch (cardNumb) {
			case 10:				
				players [who].skipStatus = true;
				AddEffectToColor(cardColor, "Reverb");
				break;
			case 11:
				reverse = !reverse;

				ToggleScaleMode();

				int difference = 0;
				if (reverse) {
					difference = who - 2;
					if (difference >= 0)
						where = difference;
					else {
						difference = Mathf.Abs (difference);
						where = players.Count - difference;
					}
				}
				else {
					difference = who + 2;
					if (difference > players.Count - 1)
						where = difference - players.Count;
					else
						where = difference;
				}
				audioTimer = (float)(AudioSettings.dspTime - globalDSPStartTime) % audioLength;
				SyncAllAudioSources();
				break;
			case 12:
				AddEffectToColor(cardColor, "Chorus");
				draw (2, players [who]);
				break;
			case 13:
				AddLowPassFilter(cardColor);
				break;
			case 14:
				AddEffectToColor(cardColor, "Flanger");
				AddLowPassFilter(cardColor);
				draw (4, players [who]);
				break;
		}
		if(cardNumb!=14)
			this.enabled = true;
	}

	public void pause(bool turnOnOff) { //turns the pause canvas on/off
		if (turnOnOff) {
			PauseAudio();
			pauseCan.SetActive (true);
			enabledStat = this.enabled;
			this.enabled = false;
		}
		else {
			ResumeAudio();
			pauseCan.SetActive (false);
			this.enabled = enabledStat;
		}
	}

	public void PauseAudio()
	{
		if (melodySource.isPlaying) melodySource.Pause();
		if (chordSource.isPlaying) chordSource.Pause();
		if (bassSource.isPlaying) bassSource.Pause();
		if (drumSource.isPlaying) drumSource.Pause();
	}

	public void ResumeAudio()
	{
		melodySource.UnPause();
		chordSource.UnPause();
		bassSource.UnPause();
		drumSource.UnPause();
	}

	public void returnHome() { //loads the home screen
		ResetAudioSystem();
		UnityEngine.SceneManagement.SceneManager.LoadScene ("Start");
	}
	public void exit() { //quits the app
		Application.Quit ();
	}
	public void playAgain() { //resets everything after a game has been played
		ResetAudioSystem();
		this.enabled = false;
		reverse = false;
		players.Clear ();
		dialogueText.text = "";
		contentHolder.GetComponent<RectTransform> ().localPosition = new Vector2 (0, 0);
		endCan.SetActive (false);
		for (int i = playerHand.transform.childCount - 1; i >= 0; i--) {
			Destroy (playerHand.transform.GetChild (i).gameObject);
		}
		Destroy(discardPileObj);
		where = 0;
		Start ();
		this.enabled = true;
	}

	public void PlayCardAudio(string color, int toneIndex)
	{
		if (lastPlayedCardToneIndex.ContainsKey(color))
		{
			lastPlayedCardToneIndex[color] = toneIndex;
		}

		string filePath = null;
		AudioSource targetSource = null;

		string scale = isMajorScale ? "maj" : "min";
		int groupIndex = audioGroupIndices[colorToInstrument[color]];
		switch (color)
		{
			case "Red": // Melody
				filePath = $"Audio/Melody/Melody_{toneIndex}_{scale}_{groupIndex}";
				targetSource = melodySource;
				break;
			case "Green": // Chord
				filePath = $"Audio/Chord/Chord {toneIndex}_{audioGroupIndices["Melody"]}_{scale}_{groupIndex}";
				targetSource = chordSource;
				break;
			case "Blue": // Bass
				filePath = $"Audio/Bass/bass {toneIndex}_{audioGroupIndices["Melody"]}_{scale}_{groupIndex}";
				targetSource = bassSource;
				break;
			case "Yellow": // Drum
				filePath = $"Audio/Drum/drum {toneIndex}_{groupIndex}";
				targetSource = drumSource;
				break;
			default:
				break;
		}
		AudioClip clip = Resources.Load<AudioClip>(filePath);
		if (clip != null)
		{

			if (!timerStarted)
			{
				StartAudioTimeline(clip);
			}

			if  (targetSource.clip != clip)
			{
				targetSource.Stop();
				targetSource.clip = clip;
			}
			
			targetSource.time = Mathf.Clamp(audioTimer, 0, clip.length - 0.01f);
			targetSource.Play();
		}
	}

	public void SyncAllAudioSources()
	{
		float syncTime = audioTimer; // Get the current global timeline time
		//double dspTime = AudioSettings.dspTime; // Unity's high-precision time


		if (melodySource.clip != null)
		{
			melodySource.time = syncTime;
			if (!melodySource.isPlaying) melodySource.Play();
		}

		if (chordSource.clip != null)
		{
			chordSource.time = syncTime;
			if (!chordSource.isPlaying) chordSource.Play();
		}
		if (bassSource.clip != null)
		{
			bassSource.time = syncTime;
			if (!bassSource.isPlaying) bassSource.Play();
		}
		if (drumSource.clip != null)
		{
			drumSource.time = syncTime;
			if (!drumSource.isPlaying) drumSource.Play();
		}
	}

	public void ResetAudioSystem()
	{
		// Stop all audio sources
		melodySource.Stop();
		chordSource.Stop();
		bassSource.Stop();
		drumSource.Stop();

		melodySource.clip = null;
		chordSource.clip = null;
		bassSource.clip = null;
		drumSource.clip = null;

		ResetEffects();
		audioTimer = 0f;
		globalDSPStartTime = 0f;

		timerStarted = false;
		isMajorScale = true;
		var keys = new List<string>(lastPlayedCardToneIndex.Keys);

		foreach (var color in keys)
		{
			lastPlayedCardToneIndex[color] = -1;
		}
	}

	private void ToggleScaleMode()
	{
		isMajorScale = !isMajorScale;

		var keys = new List<string>(lastPlayedCardToneIndex.Keys);

		foreach (var color in keys)
		{
			int toneIndex = lastPlayedCardToneIndex[color];
			if (toneIndex >= 0 && toneIndex < 10)
			{
				if (toneIndex >= 0 && toneIndex < 10)
				{
					StartCoroutine(CrossfadeAudio(null, GetAudioSourceByColor(color), GetClipByColorAndTone(color, toneIndex), 0.5f));
				}
			}
		}
	}

	private IEnumerator CrossfadeAudio(AudioSource oldSource, AudioSource newSource, AudioClip newClip, float duration)
	{
		if (oldSource != null && oldSource.isPlaying)
		{
			for (float t = 0; t < duration; t += Time.deltaTime)
			{
				oldSource.volume = Mathf.Lerp(1f, 0f, t / duration);
				yield return null;
			}
			oldSource.Stop();
			oldSource.volume = 1f;
		}

		if (newClip != null)
		{
			newSource.clip = newClip;
			newSource.time = audioTimer;
			newSource.Play();

			for (float t = 0; t < duration; t += Time.deltaTime)
			{
				newSource.volume = Mathf.Lerp(0f, 1f, t / duration);
				yield return null;
			}
		}
	}

	private AudioSource GetAudioSourceByColor(string color)
	{
		switch (color)
		{
			case "Red": return melodySource;
			case "Green": return chordSource;
			case "Blue": return bassSource;
			case "Yellow": return drumSource;
			default: return null;
		}
	}

	private AudioClip GetClipByColorAndTone(string color, int toneIndex)
	{
		string scale = isMajorScale ? "maj" : "min";
		string filePath = null;

		int groupIndex = audioGroupIndices[colorToInstrument[color]];
		switch (color)
		{
			case "Red": filePath = $"Audio/Melody/Melody_{toneIndex}_{scale}_{groupIndex}"; break;
			case "Green": filePath = $"Audio/Chord/Chord {toneIndex}_{audioGroupIndices["Melody"]}_{scale}_{groupIndex}"; break;
			case "Blue": filePath = $"Audio/Bass/bass {toneIndex}_{audioGroupIndices["Melody"]}_{scale}_{groupIndex}"; break;
			case "Yellow": filePath = $"Audio/Drum/drum {toneIndex}_{groupIndex}"; break;
		}

		return Resources.Load<AudioClip>(filePath);
	}

	private void AddEffectToColor(string color, string effect)
	{
		// Map color to reverb parameter name
		string parameterName = null;
		switch (color)
		{
			case "Red": parameterName = $"Melody_{effect}"; break;
			case "Green": parameterName = $"Chord_{effect}"; break;
			case "Blue": parameterName = $"Bass_{effect}"; break;
			case "Yellow": parameterName = $"Drum_{effect}"; break;
		}

		if (parameterName != null)
		{
			SetLevel(parameterName, 0f); // Set send level to 0 dB for reverb
			effectToInstrument[effect] = parameterName; // Track the active reverb instrument
		}
	}

	private void ResetEffect(string effect)
	{
		if (effectToInstrument.ContainsKey(effect) && effectToInstrument[effect] != null)
		{
			SetLevel(effectToInstrument[effect], -80f); // Reset effect (set send level to -80 dB)
			effectToInstrument[effect] = null;
		}
	}

	public void ResetEffects()
	{
		ResetEffect("Reverb");
		ResetEffect("Chorus");
		ResetEffect("Flanger");
		ResetLowPassFilter();
	}

	public void SetLevel(string parameterName, float value)
	{
		AudioMixer mixer = Resources.Load<AudioMixer>("AudioMixer"); // Ensure the mixer is in the Resources folder
		if (mixer != null)
		{
			mixer.SetFloat(parameterName, value);
		}
	}

	private void AddLowPassFilter(string excludedColor)
	{
		foreach (string color in colorToInstrument.Keys)
		{
			if (color != excludedColor)
			{
				string parameterName = $"{colorToInstrument[color]}_LP"; // Low-pass filter parameter name
				SetLevel(parameterName, 800f); // Set the cutoff frequency to 800 Hz
				effectToInstrument["LowPass_" + color] = parameterName; // Track the active low-pass effect
			}
		}
	}

	private void ResetLowPassFilter()
	{
		var keys = new List<string>(effectToInstrument.Keys);
		foreach (var effect in keys)
		{
			if (effect.StartsWith("LowPass_") && effectToInstrument[effect] != null)
			{
				SetLevel(effectToInstrument[effect], 22000f); // Reset cutoff frequency to 22,000 Hz
				effectToInstrument.Remove(effect);
			}
		}
	}

	public void SetMasterPitch(float pitchValue)
	{
		AudioMixer mixer = Resources.Load<AudioMixer>("AudioMixer"); // Ensure the mixer is in the Resources folder
		if (mixer != null)
		{
			mixer.SetFloat("Master_Pitch", pitchValue);

			if (pitchValue != 0){
				audioLength = originalAudioLength / pitchValue;
			}
			
		}
	}

	private void InitializeRandomAudioIndices()
	{
		audioGroupIndices["Melody"] = Random.Range(1, 3);
		audioGroupIndices["Chord"] = Random.Range(1, 3);
		audioGroupIndices["Bass"] = Random.Range(1, 3);
		audioGroupIndices["Drum"] = Random.Range(1, 3);
	}

	public void PlayDrawSound()
	{
		ChuckSubInstance chuck = GetComponent<ChuckSubInstance>();

		string chuckCode1 = @"
			// quarter note duration
			0.2615::second => dur R;
			// detune for pitch (Bb slightly sharper)
			0 => float TUNE;
			// set attack envelope
			1 => int USE_ENV;

			// patch
			HevyMetl h[3];
			// high pass (for echoes)
			HPF hpf[3];
			// reverb
			NRev r => dac; 
			.3 => dac.gain;
			0.2 => r.mix; // no reverb mix
			// delay
			Delay d => r;
			d => Gain feedback => d;
			R => d.max => d.delay;
			0.3 => d.gain;
			0.15 => feedback.gain;

			// FM operator envelope indices
			[31,31,31,31] @=> int attacks[];
			[31,31,31,31] @=> int decays[];
			[15,15,15,10] @=> int sustains[];
			[31,31,31,31] @=> int releases[];

			// connect and configure each HevyMetl instance
			for (int i; i < 3; i++) {
				h[i] => r;         // connect to reverb
				h[i] => hpf[i] => d; // connect to delay
				600 => hpf[i].freq; // high pass frequency
				0.0 => h[i].lfoDepth;
				
				if (USE_ENV) {
					for (0 => int op; op < 4; op++) {
						h[i].opADSR(op, 
						h[i].getFMTableTime(attacks[op]),
						h[i].getFMTableTime(decays[op]),
						h[i].getFMTableSusLevel(sustains[op]),
						h[i].getFMTableTime(releases[op]));
					}
				}
			}

			// play a single sound with fade-out
			fun void playSingleSound(int a, int b, int c, float vel, dur D) {
				// set the pitches
				Std.mtof(a + TUNE) => h[0].freq;
				Std.mtof(b + TUNE) => h[1].freq;
				Std.mtof(c + TUNE) => h[2].freq;
				
				// note on
				for (0 => int i; i < 3; i++) {
					vel => h[i].noteOn;
				}
				
				// sustain the sound for most of the duration
				0.7 * D => now;
				
				// fade out gradually
				0.8 => float fadeGain;
				0.1::second => dur fadeStep;
				
				while (fadeGain > 0.08) {
					for (0 => int i; i < 3; i++) {
						fadeGain => h[i].noteOn; // gradually reduce volume
					}
					fadeGain * 0.9 => fadeGain; // decrease gain (no -=)
					fadeStep => now;
				}
				
				// completely stop the sound
				for (0 => int i; i < 3; i++) {
					0.0 => h[i].noteOn;
				}
				
				// let the sound ring slightly
				0.2 * D => now;
			}

			// Play one single chord (e.g., Bb power chord)
			playSingleSound(70, 77, 70 + 12, 0.8, R);
		";

		string chuckCode2 =@"
			//Wurley

			Wurley voc => JCRev r => HPF hpf => Delay d => dac;

			0.3 => r.mix;
			1.0 => r.gain;

			600 => hpf.freq;


			0.8 => d.gain;
			d.max(0.15::second);
			d.delay(0.15::second); 

			2.9 => voc.gain;

			fun void playWurleyChord(int a, float vel, dur D) {

				Std.mtof(a) => voc.freq;
				vel => voc.noteOn;

				(D - 0.3::second) => now;

				0.8 => float fadeGain;
				0.1::second => dur fadeStep;
				for (fadeGain => float fade; fade > 0.1; fade * 0.9 => fade) {
					fade => voc.gain;
					fadeStep => now;
				}

				0.0 => voc.gain;
				0.3::second => now;
			}

			playWurleyChord(70, 0.7, 2.2615::second);
		";

		string chuckCode3 = @"
			//HevyMetl Trumpet

			HevyMetl t => NRev r => dac;

			0.3 => r.mix;
			1.0 => r.gain;

			1 => t.gain;

			[1,6,6,1] @=> int waveForms[];
			[99,85,74,99] @=> int opGains[];
			[1.0,1.0,2.9,1.0] @=> float ratios[];
			[15,14,15,15] @=> int attacks[];
			[31,20,26,14] @=> int decays[];
			[15,10,13,15] @=> int sustains[];
			[10,10,10,10] @=> int releases[];

			for (int op; op < 4; op++) {
				t.opWave(op, waveForms[op]);
				t.opGain(op, t.getFMTableGain(opGains[op]));
				t.opADSR(op, t.getFMTableTime(attacks[op]),
				t.getFMTableTime(decays[op]),
				t.getFMTableSusLevel(sustains[op]),
				t.getFMTableTime(releases[op]));
				t.opRatio(op, ratios[op]);
			}
			t.op4Feedback(0.5);

			0.005 => t.lfoDepth;

			fun void playHevyMetlChord(int a, float vel) {
				
				Std.mtof(a) => t.freq;
				vel => t.noteOn;


				0.8 => float fadeGain;
				0.1::second => dur fadeStep;
				for (fadeGain => float fade; fade > 0.1; fade * 0.9 => fade) {
					fade => t.noteOn;
					fadeStep => now;
				}

				0.0 => t.noteOn;
				0.3::second => now;
			}

			playHevyMetlChord(70, 0.7);
		";

		string[] chuckCodes = { chuckCode1, chuckCode2, chuckCode3 };
    	int randomIndex = Random.Range(0, chuckCodes.Length);

		chuck.RunCode(chuckCodes[randomIndex]);

	}
}