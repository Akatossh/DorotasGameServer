using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DorotasGame_Server_v0._4
{
    class Program
    {
        public static Hashtable clientsList = new Hashtable();
        public static Hashtable readyPlayers = new Hashtable();
        public static Hashtable sentQuestionsPlayers = new Hashtable();
        public static Hashtable sentAnswersPlayers = new Hashtable();
        public Hashtable PlayersPoints = new Hashtable();

        public List<int> intList = new List<int>();
        public List<string> playersNames = new List<string>();
        public List<string> readyPlearsList = new List<string>();

        
        Server_comunication serverComunication = new Server_comunication();

        public bool sendedAnswerBack = false;

        Game game = new Game();

        public void Dorotasgame()
        {
            game.amountOfquestions = 0;
            game.amountOfAnswers = 0;

            Thread.Sleep(100);


            while (true)
            {
                if (game.AmountOfPlayers > 1)
                    break;
                Thread.Sleep(200);
            }


            while (true)
            {
                if ((game.AmountOfPlayers) == readyPlayers.Count)
                    break;
                Thread.Sleep(200);
            }

            while (true)
            {
                if (((game.AmountOfPlayers) == sentQuestionsPlayers.Count) && ((game.AmountOfPlayers) == sentAnswersPlayers.Count))
                    break;

                Thread.Sleep(200);
            }





            //a
            broadcast("Server|MainGameStart|Server", "server", false, "statusPlayers", 0);



            int randReadingPlayer = game.RandomNumber(0, game.AmountOfPlayers);
            int randQuest = game.RandomNumber(0, game.amountOfquestions);
            int randAnswer = 0;
            

            //wyslij pytanie do gracza 
            broadcast(game.questionsList[randQuest], "Question", false, "SendQuestion", randReadingPlayer);
            game.removingQuestion(randQuest);

            //głowna faza gry
            while (true)
            {

                //wyslij slowa do pozostalych graczy
                for (int i = 0; i != game.AmountOfPlayers; i++)
                {
                    randAnswer = game.RandomNumber(0, game.amountOfAnswers);
                    if (randReadingPlayer != i)
                    {
                        broadcast(game.ansewrsList[randAnswer], "Answer", false, "SendQuestion", i);
                       
                        game.removingAnswer(randAnswer);
                    }

                }

                //czekaj na odpowiedz od wylosowanego gracza
                while (true)
                {
                    if (sendedAnswerBack == true)
                    {
                        sendedAnswerBack = false;
                        break;
                    }


                    Thread.Sleep(200);
                }


                broadcast("Server|ResetLayouts|Server", "server", false, "statusPlayers", 0);
                Thread.Sleep(500);

                //sprawdz czy gra sie nie skonczyla


                //ustaw kolejnego gracza jako aktywny
                randReadingPlayer++;
                if (randReadingPlayer == game.AmountOfPlayers)
                    randReadingPlayer = 0;
                randQuest = game.RandomNumber(0, game.amountOfquestions);

                //wyslij pytanie do gracza 
                broadcast(game.questionsList[randQuest], "Question", false, "SendQuestion", randReadingPlayer);
                game.removingQuestion(randQuest);

                //sprawdz czy gra sie nie skonczyla
                if (game.endGame() == "END")
                {
                    broadcast("Server|EndGame|koniec pytan, koniec gry", "server", false, "statusPlayers", 0);
                }

                //Thread.Sleep(500);
            }
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            p.Server();
        }

        void Server()
        {
            
            game.AmountOfPlayers = 0;
            int port = 8003;
            string IpAddress = "0.0.0.0";
            Socket ServerListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(IpAddress), port);

            ServerListener.Bind(ep);
            ServerListener.Listen(100);

            int counter = 0;
            game.AmountOfPlayers = 0;

            while (true)
            {
                counter++;

                Socket Clientsocket = default(Socket);
                Clientsocket = ServerListener.Accept();

                Thread t = new Thread(new ThreadStart(() => userThred(Clientsocket, counter)));
                t.Start();
            }
        }

        public void userThred(Socket Clientsocket, int numerGracza)
        {
            string receivedData = null;
            string receivedCommand = null;
            string receivedMessage = null;
            string receivedPlayerName = null;

            bool newConnection = true;

            game.AmountOfPlayers++;

            while (true)
            {
                byte[] buff = new byte[4096];
                int size = Clientsocket.Receive(buff);

                receivedData = System.Text.Encoding.ASCII.GetString(buff);
                if (newConnection == true)
                {
                    clientsList.Add(receivedData, Clientsocket);
                    newConnection = false;

                }

                receivedCommand = Player.cutOfCommand(receivedData);
                receivedMessage = Player.cutOfMessage(receivedData);
                receivedPlayerName = Player.cutOfPlayerName(receivedData);

              

                switch (receivedCommand)
                {
                    case "SetName":
                        newPlayerConnected(receivedPlayerName);
                        break;
                    case "Chat":
                        broadcast(receivedMessage, receivedPlayerName, false, "Chat", 0);
                        break;
                    case "Ready":
                        playerIsReady(receivedMessage, receivedPlayerName);
                        break;
                    case "question":
                        playerSentQuestions(receivedMessage, receivedPlayerName);
                        break;
                    case "answer":
                        playerSentAnswers(receivedMessage, receivedPlayerName);
                        break;
                    case "ChosenPlayer":
                        givePointsToPlayer(receivedMessage, receivedPlayerName);
                        break;
                    case "ReRoll":
                        ReRoll(receivedMessage, receivedPlayerName);
                        break;
                    default:
                        break;
                }
                receivedData = "server respond: " + receivedData;
                size = receivedData.Length;
            }
        }

        private void ReRoll(string receivedMessage, string receivedPlayerName)
        {
            game.ReRoll(receivedMessage);

            int randAnswer = game.RandomNumber(0, game.amountOfAnswers);

            for (int i = 0; i != game.AmountOfPlayers; i++)
            {
                if (playersNames[i].ToString() == receivedPlayerName)
                {
                    broadcast(game.ansewrsList[randAnswer], "Answer", false, "ReRollBack", i);
                }
            }

            game.removingAnswer(randAnswer);

        }

        private void givePointsToPlayer(string receivedMessage, string receivedPlayerName)
        {
            string temp = null;
            int index = receivedMessage.IndexOf(";");
            receivedMessage = receivedMessage.Substring(0, index);

            string key = null;
            int temp2 = 0;
            foreach (DictionaryEntry Item in PlayersPoints)
            {

                if (Item.Key.ToString().Equals(receivedMessage))
                {
                    temp2 = (int)Item.Value;
                    key = Item.Key.ToString();
                    temp2 = temp2 + 1;

                }
            }

            PlayersPoints[key] = temp2;
            string msg = serverComunication.makePointsString(PlayersPoints);
            broadcast("Server|Points|" + msg, "server", false, "statusPlayers", 0);
            sendedAnswerBack = true;

        }

        private void newPlayerConnected(string playerName)
        {
            playersNames.Add(playerName);
            int intivalue = 0;
            PlayersPoints.Add(playerName, intivalue);
            string msg = serverComunication.makePlayersStatusString("gamePhase1", playersNames);

            broadcast(msg, "server", false, "statusPlayers", 0);
        }

        private void playerSentAnswers(string receivedMessage, string receivedPlayerName)
        {
            broadcast("gracz wyslal slowa", receivedPlayerName, false, "Chat", 0);
            sentAnswersPlayers.Add(receivedPlayerName, receivedMessage);
            game.addingAnswers(receivedMessage);
        }

        private void playerSentQuestions(string receivedMessage, string receivedPlayerName)
        {
            broadcast("gracz wyslal pytania", receivedPlayerName, false, "Chat", 0);
            sentQuestionsPlayers.Add(receivedPlayerName, receivedMessage);
            game.addingQuestions(receivedMessage);
        }

        public void playerIsReady(string receivedMessage, string receivedPlayerName)
        {

            broadcast("gracz jest gotowy do gry", receivedPlayerName, false, "Chat", 0);
            readyPlayers.Add(receivedPlayerName, receivedMessage);
            readyPlearsList.Add(receivedPlayerName);
            string msg = serverComunication.makePlayersStatusString("gamePhase2", readyPlearsList);
            broadcast(msg, "server", false, "statusPlayers", 0);
            serverComunication.makePlayersStatusString("gamePhase2", playersNames);
        }



        public static void broadcast(string msg, string uName, bool flag, string command, int rand)
        {
            int i = 0;
            foreach (DictionaryEntry Item in clientsList)
            {
                Socket broadcastSocket;
                broadcastSocket = (Socket)Item.Value;

                if (command == "statusPlayers")
                {
                    broadcastSocket.Send(Encoding.ASCII.GetBytes(msg));
                }

                if (flag == true)
                {
                    //broadcastSocket.Send(Encoding.ASCII.GetBytes(uName + " says : " + msg));
                }
                else
                {
                    if (command == "Chat")
                    {
                        broadcastSocket.Send(Encoding.ASCII.GetBytes("Server|Chat|" + uName + ": " + msg));
                    }
                    else if (command == "SendQuestion")
                    {
                        if (rand == i)
                            broadcastSocket.Send(Encoding.ASCII.GetBytes("Server|" + uName + "|" + msg));

                    }
                    else if (command == "ReRollBack")
                    {
                        if (rand == i)
                            broadcastSocket.Send(Encoding.ASCII.GetBytes("Server|" + "ReRollBack" + "|" + msg));
                    }
                    else if (command == "SendAnswers")
                    {
                        if (rand != i)
                            broadcastSocket.Send(Encoding.ASCII.GetBytes("Server|" + uName + "|" + msg));
                    }
                    else
                    {

                    }

                }
                i++;

            }
        }  //end broadcast function

    }
}
