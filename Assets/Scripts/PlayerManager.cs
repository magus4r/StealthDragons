using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//the "using Mirror" assembly reference is required on any script that involves networking
using Mirror;
using TMPro;

//the PlayerManager is the main controller script that can act as Server, Client, and Host (Server/Client). Like all network scripts, it must derive from NetworkBehaviour (instead of the standard MonoBehaviour)
public class PlayerManager : NetworkBehaviour
{
    //Card1 and Card2 are located in the inspector, whereas PlayerArea, EnemyArea, and DropZone are located at runtime within OnStartClient()
     
    public GameObject Card1;
    public GameObject Card2;

    public GameObject PlayerArea;
    public GameObject EnemyArea;
    public GameObject DropZone1;
    public GameObject DropZone2;

    public GameObject Deck1;
    public GameObject Deck2;
    public GameObject cardinDeck1;
    public GameObject cardinDeck2;
    public GameObject cardinDeck3;
    public GameObject cardinDeck4;
    public int deckSize1;
    public int deckSize2;
   // public TMP_Text deckSizeText1;
   // public TMP_Text deckSizeText2;


    //the cards List represents our deck of cards
    List<GameObject> cards = new List<GameObject>();    // cards first drop when game begins
    List<GameObject> cardsinDeck = new List<GameObject>();   // cards top of deck

    public override void OnStartClient()
    {
        base.OnStartClient();

        PlayerArea = GameObject.Find("PlayerArea");
        EnemyArea = GameObject.Find("EnemyArea");
        DropZone1 = GameObject.Find("DropZone1");
        DropZone2 = GameObject.Find("DropZone2");
        Deck1 = GameObject.Find("Deck1");
        Deck2 = GameObject.Find("Deck2");

    }

    //when the server starts, store Card1 and Card2 in the cards deck. Note that server-only methods require the [Server] attribute immediately preceding them!
    [Server]
    public override void OnStartServer()
    {
        cards.Add(Card1);
        cards.Add(Card2);

        cardsinDeck.Add(cardinDeck1);
        cardsinDeck.Add(cardinDeck2);
        cardsinDeck.Add(cardinDeck3);
        cardsinDeck.Add(cardinDeck4);
    }
    
    //Commands are methods requested by Clients to run on the Server, and require the [Command] attribute immediately preceding them. CmdDealCards() is called by the DrawCards script attached to the client Button
    [Command]
    public void CmdDealCards()
    {
        StartCoroutine(DealCards());  // deal cards every 1sec.

    }
    IEnumerator DealCards()  // start game go draw first cards
    {
        //(5x) Spawn a random card from the cards deck on the Server, assigning authority over it to the Client that requested the Command. Then run RpcShowCard() and indicate that this card was "Dealt"
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(1);
            GameObject card = Instantiate(cards[Random.Range(0, cards.Count)], new Vector2(0, 0), Quaternion.identity);
            NetworkServer.Spawn(card, connectionToClient);
            RpcShowCard(card, "Dealt");
        }
    }
    [Command]
    public void CmdDealDeckCards()
    {
        for (int i = 0; i < cardsinDeck.Count; i++)
        {
            GameObject Deckcard = Instantiate(cardsinDeck[i], new Vector2(0, 0), Quaternion.identity);
            NetworkServer.Spawn(Deckcard, connectionToClient);
            RpcShowCards(Deckcard, "Dealt");
            RpcShowText();
        }
    }
    [ClientRpc]
    void RpcShowCards(GameObject Deckcard, string type)   // show deck 
    {
        if (type == "Dealt")
        {
            if (hasAuthority)
            {
                Deckcard.transform.SetParent(Deck1.transform, false);
                
            }
            else
            {
                Deckcard.transform.SetParent(Deck2.transform, false);
               
            }
        }
    }
    [ClientRpc]
    void RpcShowText()
    {
      
            if (hasAuthority)
            {
                deckSize1 = 40;
           //     deckSizeText1.text = deckSize1.ToString();
            }
            else
        {
            deckSize2 = 29;
         //   deckSizeText2.text = deckSize2.ToString();
        }
       
    }

    //PlayCard() is called by the DragDrop script when a card is placed in the DropZone, and requests CmdPlayCard() from the Server
    public void PlayCard(GameObject card)
    {
        CmdPlayCard(card);
    }

    //CmdPlayCard() uses the same logic as CmdDealCards() in rendering cards on all Clients, except that it specifies that the card has been "Played" rather than "Dealt"
    [Command]
    void CmdPlayCard(GameObject card)
    {
        RpcShowCard(card, "Played");
    }

    //ClientRpcs are methods requested by the Server to run on all Clients, and require the [ClientRpc] attribute immediately preceding them
    [ClientRpc]
    void RpcShowCard(GameObject card, string type)
    {
        //if the card has been "Dealt," determine whether this Client has authority over it, and send it either to the PlayerArea or EnemyArea, accordingly. For the latter, flip it so the player can't see the front!
        if (type == "Dealt")
        {
            if (hasAuthority)
            {
                card.transform.SetParent(PlayerArea.transform, false);
            }
            else
            {
                card.transform.SetParent(EnemyArea.transform, false);
                card.GetComponent<CardFlipper>().Flip();
            }
        }
        //if the card has been "Played," send it to the DropZone. If this Client doesn't have authority over it, flip it so the player can now see the front!
        else if (type == "Played")
        {
            if (hasAuthority)
            {
                card.transform.SetParent(DropZone1.transform, false);
            }
            else
            {
                card.transform.SetParent(DropZone2.transform, false);   //2nd zone for oponent 
                card.GetComponent<CardFlipper>().Flip();
            }
            //         card.transform.SetParent(DropZone1.transform, false);
            //         if (!hasAuthority)
            //         {
            //             card.GetComponent<CardFlipper>().Flip();
            //         }
        }
    }

    //CmdTargetSelfCard() is called by the TargetClick script if the Client hasAuthority over the gameobject that was clicked
    [Command]
    public void CmdTargetSelfCard()
    {
        TargetSelfCard();
    }

    //CmdTargetOtherCard is called by the TargetClick script if the Client does not hasAuthority (err...haveAuthority?!?) over the gameobject that was clicked
    [Command]
    public void CmdTargetOtherCard(GameObject target)
    {
        NetworkIdentity opponentIdentity = target.GetComponent<NetworkIdentity>();
        TargetOtherCard(opponentIdentity.connectionToClient);
    }

    //TargetRpcs are methods requested by the Server to run on a target Client. If no NetworkConnection is specified as the first parameter, the Server will assume you're targeting the Client that hasAuthority over the gameobject
    [TargetRpc]
    void TargetSelfCard()
    {
        Debug.Log("Targeted by self!");
    }

    [TargetRpc]
    void TargetOtherCard(NetworkConnection target)
    {
        Debug.Log("Targeted by other!");
    }

    //CmdIncrementClick() is called by the IncrementClick script
    [Command]
    public void CmdIncrementClick(GameObject card)
    {
        RpcIncrementClick(card);
    }

    //RpcIncrementClick() is called on all clients to increment the NumberOfClicks SyncVar within the IncrementClick script and log it to the debugger to demonstrate that it's working
    [ClientRpc]
    void RpcIncrementClick(GameObject card)
    {
        card.GetComponent<IncrementClick>().NumberOfClicks++;
        Debug.Log("This card has been clicked " + card.GetComponent<IncrementClick>().NumberOfClicks + " times!");
    }
}
