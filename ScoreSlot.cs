
/*
    实现了游戏的基本逻辑功能：当某个槽内卡牌数量达到五张，计算对应得分并播放动画
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.U2D;
using UnityEngine.Animations;









public class ScoreSlot : MonoBehaviour
{

	[SerializeField]
	private ParticleSystem.MainModule main;
	public  card CurrentCard;
	private Animator FlipAnimator;
	public card PreviousCard;
	[HideInInspector]
	private int[] _numstack=new int[5];
	public int[] numstack{
		get {return this._numstack;}
	}
	[HideInInspector]
	private int[] _suitstack=new int[5];
	public int[] suitstack{
		get {return this._suitstack;}
	}
	[HideInInspector]
	private int _index=-1;
	public  int index{
		get {return this._index;}
	}
	[HideInInspector]
	public static int score=0;

	public static GameMaster gm;

	private card cardToMove;

	[SerializeField]
	public card[] Cards;
	
	
	[SerializeField]
	private Burster _ScoreBurst;
	public Burster ScoreBurst{
		get{return _ScoreBurst;}
	}


	[SerializeField]
	private Transform[] _tripleLocations;
	public Transform[] tripleLocations{
		get{return this._tripleLocations;}
	}

	private Animator ScoreAnimator;
	
	//private ParticleSystem.MinMaxGradient normalColor;
	//public ParticleSystem.MinMaxGradient goldenColor;

	[SerializeField]
	private Transform handvalueNum;
	[SerializeField]
	private Transform handvalueText;

	private SpriteRenderer NumRenderer;
	private SpriteRenderer TextRenderer;

	[SerializeField]
	private SpriteAtlas scoreatlas;

	//public ParticleSystem.MinMaxCurve goldenSize;

	//public Text handValueText;

	private string MoveAnim;
	private RectTransform currentcardpos;

	Rigidbody2D physical;
	public float vxMin=-2f;
	public float vxMax=2f;
	public float vyMin=0f;
	public float vyMax=2f;
	public float gravityMin=3f;
	public float gravityMax=5f;
	private float vx;
	private float vy;
	private float gravity;
	

	private static string[] suits=new string[5]{"hearts","diamonds","spades","clubs","Joker"};
	public  void onClick(){
		if (!GameMaster.canRespond || !gm.started){
			return;
		}
		_index+=1;
		if (index>4 || index <0){
			Debug.Log("No response.");
			return;
		}
		Debug.Log("Move to slot #"+index.ToString());
		numstack[index]=CurrentCard.pNum+1;// 1,2,3,...
		suitstack[index]=CurrentCard.pSuit;
		Cards[index].updateValue(CurrentCard.pNum,CurrentCard.pSuit);
		MoveAnim="Card"+(index+1)+"Move";
		cardToMove=Cards[index];
		ScoreAnimator.SetBool(MoveAnim,true);

		cardToMove.ReDraw();
		//ScoreAnimator.enabled=false;
		//cardToMove.Move(MoveAnim,CurrentCard.transform,cardToMove.transform);
		//var clip=cardToMove.Move(MoveAnim,CurrentCard.gameObject.GetComponent<RectTransform>(),cardToMove.gameObject.GetComponent<RectTransform>());
		StartCoroutine(MoveAnimReseter(MoveAnim));
		if (index <4){
			if(PreviousCard==null){
				Debug.LogError("PreviousCard not referenced at "+gameObject.name);
			}
			PreviousCard.updateValue(CurrentCard.pNum,CurrentCard.pSuit);
			GameMaster.canReturn=true;
			CurrentCard.hideWild();
			CurrentCard.updateValue(-1,-1);
			//CurrentCard.ReDraw();
			CurrentCard.hide();
			//FlipAnimator.SetBool("ToFlip",false);
			Debug.Log("reset flip animator");
			GameMaster.currentSlot=this;
            gm.streak = 0;
			//gm.Draw();
		}
		else{
			CurrentCard.updateValue(-1,-1);
			CurrentCard.hideWild();
			//CurrentCard.ReDraw();
			CurrentCard.hide();
			//FlipAnimator.SetBool("ToFlip",false);
			GameMaster.canReturn=false;
			PreviousCard.updateValue(-1,-1);
			GameMaster.currentSlot=null;
			score=calScore(numstack,suitstack);
			if (score>GameMaster.bestScore){
				GameMaster.bestScore=score;
			}

			if (score==0){
				//StartCoroutine(BustedDelay());
				GameMaster.Gameover();

					//Init();
			}
			else{




				if(score==1000){
					gm.audioManager.PlaySound("royalWin");
				}
				else{
				gm.audioManager.PlaySound("win");
			}
				//Debug.Log(ScoreAnimator.gameObject.name);
				ScoreAnimator.enabled=true;
				ScoreAnimator.SetBool("Play",true);
				//Debug.Log("Animation");
				//Debug.Log("Hand Value: "+score);
				GameMaster.gameScore+=score;
                GameMaster.gameScore += gm.streak * 500;
                gm.streak += 1;
				if (SkillzCrossPlatform.IsMatchInProgress ()) 
			{ 
    		SkillzCrossPlatform.UpdatePlayersCurrentScore(GameMaster.gameScore); 
			}
				//Init();
				//gm.Draw();
			}
		}
		gm.Draw();


		return;

	}

	public IEnumerator DelayBeforeBusted(){
		yield return new WaitForSeconds(0.5f);
		GetComponent<Animator>().enabled=false;

	}



	IEnumerator BustedDelay(){
        Debug.Log("Busted Delay...");
        yield return new WaitForSeconds(2);
        GameMaster.Gameover();
    }

    IEnumerator MoveAnimReseter(string MoveAnim){
    	yield return new WaitForSeconds(0.2f);
    	ScoreAnimator.SetBool(MoveAnim,false);
    	Debug.Log("Reset move anim: "+MoveAnim);
    }
	

	public  int calScore(int[] numstack, int[] suitstack){

	
		bool Joker=false;
		Dictionary<int,int>numDict=new Dictionary<int,int>();
		Dictionary<int,int>suitDict=new Dictionary<int,int>{
			[0]=0,
			[1]=0,
			[2]=0,
			[3]=0,
		};

		// check Joker and swap to last pos
		for (int i=0;i<5;i++){
			if (suitstack[i]==4){
				Joker=true;
				int temp=numstack[i];
				numstack[i]=numstack[4];
				numstack[4]=temp;
				temp=suitstack[i];
				suitstack[i]=suitstack[4];
				suitstack[4]=temp;
				break;
			}
		}
		
		//sort
		int L=5;
		if (Joker){
			L=4;
		}
		for(int i=0;i< L;i++){
			for(int j=i+1;j<L;j++){
				if (numstack[j]<numstack[i]){
					int temp=numstack[i];
					numstack[i]=numstack[j];
					numstack[j]=temp;
					temp=suitstack[i];
					suitstack[i]=suitstack[j];
					suitstack[j]=temp;
				}
			}
		}


		/*for(int i=0;i<5;i++){
			Debug.Log(suits[suitstack[i]]+numstack[i]);
		}*/

		for(int i=0;i<5;i++){
			if (suitstack[i]!=4){
				if (numDict.ContainsKey(numstack[i])){
					numDict[numstack[i]]+=1;
				}
				else{
					numDict[numstack[i]]=1;
				}
				if (suitDict.ContainsKey(suitstack[i])){
					suitDict[suitstack[i]]+=1;
				}
				else{
					suitDict[suitstack[i]]=1;
				}
			}
		}
		
		//Royal Flush
		bool isFlush(){
			return suitDict.ContainsValue(5) ||(suitDict.ContainsValue(4) && Joker);
		}

		bool isStraight(){
			if(!Joker){

				for(int i=0;i<4;i++){
					if (numstack[i+1]!=numstack[i]+1){
						//Debug.Log("No Straight");
						return false;
				}

			}
			return true; 
			}
			int count=0;
			for(int i=0;i<3;i++){
				var diff=numstack[i+1]-numstack[i];
				if (diff>2 || diff <1){
					return false;
				}
				if(diff==2){
					count+=1;
				}
				if (count>1){
					return false;
				}
			}
			return true;

		}

		bool isTentoAce(){
			if(!Joker){
				/*
				foreach(int i in numstack){
					Debug.Log(i);
				}
				*/
				int[] AceSequence=new int[5]{1,10,11,12,13};
				if (Enumerable.SequenceEqual(numstack,AceSequence) ){
					//Debug.Log("Ace Straight");
					return true;
				}
				else {
					//Debug.Log("No Ace Straight");
					return false;
				}
			}
			if (Enumerable.SequenceEqual(numstack,new int[5]{1,10,11,12,1})){
				//Debug.Log("Ace Straight");
				return true;
			}
			if (Enumerable.SequenceEqual(numstack,new int[5]{1,10,11,13,1})){
				//Debug.Log("Ace Straight");
				return true;
			}
			if (Enumerable.SequenceEqual(numstack,new int[5]{1,10,12,13,1})){
				//Debug.Log("Ace Straight");
				return true;
			}
			if (Enumerable.SequenceEqual(numstack,new int[5]{1,11,12,13,1})){
				//Debug.Log("Ace Straight");
				return true;
			}
			if (Enumerable.SequenceEqual(numstack,new int[5]{10,11,12,13,1})){
				//Debug.Log("Ace Straight");
				return true;
			}
			//Debug.Log("No Ace Straight.");
			return false;
		}
		bool Flush=isFlush();
		bool Straight=isStraight();
		bool TentoAce=isTentoAce();		


		// Royal
		if (Flush && TentoAce){
			Debug.Log("Royal flush");
			TextRenderer.sprite=scoreatlas.GetSprite("royalFlush");
			NumRenderer.sprite=scoreatlas.GetSprite("+1000");
			main.startColor=gm.goldenColor;
			main.startSize=gm.largeSize;
			StartCoroutine(soundPlayer("fw_royal"));
            gm.handRecorder[0] += 1;
			
			return gm.handrule.values[9];
		}

		//Straigt Flush
		if (Flush && Straight ){
			Debug.Log("Straight Flush.");
			TextRenderer.sprite=scoreatlas.GetSprite("straightFlush");
			NumRenderer.sprite=scoreatlas.GetSprite("+800");
			main.startColor=gm.normalColor;
			main.startSize=gm.normalSize;
			StartCoroutine( soundPlayer("fw_1+3"));
            gm.handRecorder[1] += 1;

            return gm.handrule.values[8];
		}

		// 4 of a kind
		if (numDict.ContainsValue(4) || (numDict.ContainsValue(3) && Joker)){
			Debug.Log("4 of a kind");
			TextRenderer.sprite=scoreatlas.GetSprite("quads");
			NumRenderer.sprite=scoreatlas.GetSprite("+700");
			main.startColor=gm.normalColor;
			main.startSize=gm.normalSize;
			StartCoroutine( soundPlayer("fw_1+3"));
            gm.handRecorder[2] += 1;

            return gm.handrule.values[7];
		}

		// Full house
		int Pairs=0;
		foreach(int v in numDict.Values){
			if (v==2){
				Pairs+=1;
			}
		}

		if ((numDict.ContainsValue(3) && numDict.ContainsValue(2))  || (Joker && Pairs==2))
		{
			Debug.Log("Full House");
			TextRenderer.sprite=scoreatlas.GetSprite("fullHouse");
			NumRenderer.sprite=scoreatlas.GetSprite("+600");
			main.startColor=gm.normalColor;
			main.startSize=gm.largeSize;
			StartCoroutine( soundPlayer("fw_single"));
            gm.handRecorder[3] += 1;


            return gm.handrule.values[6];
		}

		// Straight
		if (Straight|| TentoAce){
			Debug.Log("Straight");
			TextRenderer.sprite=scoreatlas.GetSprite("straight");
			NumRenderer.sprite=scoreatlas.GetSprite("+500");
			main.startColor=gm.normalColor;
			main.startSize=gm.largeSize;
			StartCoroutine( soundPlayer("fw_single"));
            gm.handRecorder[4] += 1;


            return gm.handrule.values[5];
		}

		// Flush
		if( Flush){
			Debug.Log("Flush");
			TextRenderer.sprite=scoreatlas.GetSprite("flush");
			NumRenderer.sprite=scoreatlas.GetSprite("+400");
			main.startColor=gm.normalColor;
			main.startSize=gm.largeSize;
			StartCoroutine( soundPlayer("fw_single"));
            gm.handRecorder[5] += 1;


            return gm.handrule.values[4];
		}

		// 3 of a kind
		if (numDict.ContainsValue(3) || (Joker && numDict.ContainsValue(2))){
			Debug.Log("3 of a kind.");
			TextRenderer.sprite=scoreatlas.GetSprite("trips");
			NumRenderer.sprite=scoreatlas.GetSprite("+300");
			main.startSize=gm.normalSize;
			main.startColor=gm.color_300;
			StartCoroutine( soundPlayer("fw_single"));
            gm.handRecorder[6] += 1;


            return gm.handrule.values[3];
		}

		//2 pairs
		if (Pairs==2 || (Joker && Pairs==1)){
			Debug.Log("Two pair.");
			TextRenderer.sprite=scoreatlas.GetSprite("2pairs");
			NumRenderer.sprite=scoreatlas.GetSprite("+200");
			main.startSize=gm.normalSize;
			main.startColor=gm.color_200;
			StartCoroutine( soundPlayer("fw_single"));
            gm.handRecorder[7] += 1;

            return gm.handrule.values[2];
		}

		//1 pair
		if (Pairs==1 || Joker){
			Debug.Log("One pair.");
			TextRenderer.sprite=scoreatlas.GetSprite("1pair");
			NumRenderer.sprite=scoreatlas.GetSprite("+100");
			main.startSize=gm.normalSize;
			main.startColor=gm.color_100;
			StartCoroutine( soundPlayer("fw_single"));
            gm.handRecorder[8] += 1;

            return gm.handrule.values[1];
		}
		Debug.Log("High Card");
        gm.handRecorder[9] += 1;

        return gm.handrule.values[0];
	
	}

	IEnumerator soundPlayer(string sound){
		yield return new WaitForSeconds(0.5f);
		Debug.Log("Play: "+sound);
		gm.audioManager.PlaySound(sound);
	}

	

	void Awake(){
		if (gm==null){
			gm=GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();
		}

	}


	public  void Init(){
		_index=-1;
		_numstack=new int[5];
		_suitstack=new int[5];
		//score=0;
		foreach(card Card in this.Cards){
			Card.Reset();
			//Card.updateSprite(-1,-1);
		}
		Debug.Log("Init slot");
		//Debug.Log("Init");
	}
	
	public void Return(){
		Cards[index].Reset();
		//Cards[index].updateSprite(-1,-1);
		_index-=1;
		GameMaster.canReturn=false;
		GameMaster.currentSlot=null;
		return;
	}


	IEnumerator EffectAppear(Transform pos,GameObject prefab,float start,float end){
				yield return new WaitForSeconds(Random.Range(start,end));
				Debug.Log("effect apear");
				GameObject Effect=Instantiate(prefab,pos.position, Quaternion.identity);
				StartCoroutine(EffectDestroy(Effect));
				//Destroy(Effect);

			}

	IEnumerator EffectDestroy(GameObject Effect){
				yield return new WaitForSeconds(3f);
				Destroy(Effect);
	}



	public void Burst(){
		
		//var main=ScoreBurst.confetti.gameObject.GetComponent<ParticleSystem>().main;
		if(score==1000){
			foreach(Transform pos in gm.RoyalLocation){
				Debug.Log("Royal Effect");
				StartCoroutine(EffectAppear(pos,gm.RoyalPrefab,0f,gm.RoyalDelaytime));
			}
			
		}
		else{
			if(score==700 || score==800){
				foreach(Transform pos in tripleLocations){
					StartCoroutine(EffectAppear(pos,gm.SmallPrefab,0.3f,0.3f));
				}
			}
		}
		ScoreBurst.confetti.gameObject.SetActive(false);
		ScoreBurst.GetComponent<Animator>().SetBool("Move",true);
		Debug.Log("Burst");
		Init();
		ScoreBurst.confetti.gameObject.SetActive(true);

	}

	public void ResetBurst(){
		ScoreAnimator.SetBool("Play",false);
		Debug.Log("Start reseting");
		//Init();
	}





    void Start()
    {
    	Init();
    	score=0;
    	ScoreAnimator=GetComponent<Animator>();
    	FlipAnimator=CurrentCard.GetComponent<Animator>();
    	if (ScoreAnimator==null){
    		Debug.LogError(this.name+"No Animator!");
    	}
    	else{
    		//Debug.Log(ScoreAnimator.name);
    	}
    	if(handvalueText==null){
    		Debug.LogError("Hand Value Text not referenced!");
    	}
    	else{
    		TextRenderer=handvalueText.GetComponent<SpriteRenderer>();
    	}
    	if(handvalueNum==null){
    		Debug.LogError("Hand Value Num not referenced!");
    	}
    	else{
    		NumRenderer=handvalueNum.GetComponent<SpriteRenderer>();
    	}

    	main=ScoreBurst.confetti.GetComponent<ParticleSystem>().main;
    	//normalColor=main.startColor;
    	
    }


    public void RedrawMoveCard(){
    	//cardToMove.ReDraw();
    	Debug.Log("Redraw Card to Move");
    }

    public void ResetCardMove(){
    	
    	//gm.Draw();

    }

    public card LastCard(){
    	return Cards[index];
    }




    public void Busted(){
    	//GetComponent<Animator>().enabled=false;
    	StartCoroutine(DelayBeforeBusted());
    	foreach(card c in Cards){
    		physical=c.gameObject.GetComponent<Rigidbody2D>();
    		if (physical==null){
    			Debug.LogError("Rigidbody2D not assigned!"+c.name+this.name);
    			return;
    		}
    		vx=Random.Range(vxMin,vxMax);
    		vy=Random.Range(vyMin,vyMax);
    		gravity=Random.Range(gravityMin,gravityMax);
    		physical.velocity=new Vector2(vx,vy);
    		physical.gravityScale=gravity;
    	}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
