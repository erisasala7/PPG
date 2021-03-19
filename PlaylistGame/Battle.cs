using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using PlaylistGame;

namespace PlaylistGame
{
    public class Battle
    {
     
        public List<UserBattleInfo> user_infos = new List<UserBattleInfo>();
        public List<UserBattleInfo> blocked_users = new List<UserBattleInfo>();
        public List<UserBattleInfo> active_users= new List<UserBattleInfo>();
        public string log;
        public int currentAdminId = -1;
        //int battleCountdown = -1;
        bool battleActive = false;

        public Battle() {

        }
        Task timer;

        public string joinBattle( UserBattleInfo user ) {
            if (!battleActive) {
                battleActive = true;
                timer = Task.Run(startTimerAsync);
            }
            active_users.Add(user);
            //add active user

            timer.Wait();
            return log;
        }
        public virtual void startTimerAsync()
        {
            Console.WriteLine("Battle will start soon. ");
            for (int i = 15; i >= 0; i--)
            {
                Console.WriteLine(i);
                Thread.Sleep(1000);
            }
            start_tournament();
        }

        public void start_tournament() {
            int playerCount = active_users.Count;
            if (playerCount > 1)
            {
                for (int i = 0; i < playerCount - 1; i++)
                {
                    for (int j = i + 1; j < playerCount; j++)
                    {
                        bool block = false;
                        foreach (UserBattleInfo b_user in blocked_users)
                        {
                            if (active_users[i] == b_user || active_users[j] == b_user)
                            {
                                block = true;
                            }
                        }
                        if (!block)
                        {
                            fight(active_users[i], active_users[j]);
                        }
                    }
                }
                log += "Results: \r\n";
                int highest = -1;
                foreach (UserBattleInfo player in active_users)
                {
                    log += "  " + player.username + ": " + player.battle_score;
                    if (player.battle_score > highest)
                    {
                        highest = player.battle_score;
                    }
                }
                log += "\r\n";
                List<UserBattleInfo> winnerList = new List<UserBattleInfo>();
                foreach (UserBattleInfo player in active_users)
                {
                    if (player.battle_score == highest) { winnerList.Add(player); }
                }
                if (winnerList.Count > 1)
                {
                    log += "Our tournament ended in a draw between ";
                    foreach (UserBattleInfo player in winnerList)
                    {
                        log += player.username + " and ";
                    }
                    log = log.Remove(log.Length - 5);
                    log += ". What a travesty!!!\r\n";
                    currentAdminId = -1;
                }
                else
                {
                    log += winnerList[0].username + " is the Winner! Congrats!\r\n";
                    currentAdminId=finishResult(winnerList);
                    //currentAdminId = DB_Tools.nameToUserid(winnerList[0].username);
                    //DB_Tools.incrementUserWin(DB_Tools.nameToUserid(winnerList[0].username));
                }
            }
            else {
                log += active_users[0].username + " is the Winner! Congrats!\r\n";
                currentAdminId=finishResult(active_users);
                //currentAdminId = DB_Tools.nameToUserid(active_users[0].username);
                //DB_Tools.incrementUserWin(DB_Tools.nameToUserid(active_users[0].username));
            }
            foreach (UserBattleInfo user in active_users) {
                user.battle_score = 0;
            }
            blocked_users = new List<UserBattleInfo>();
            active_users = new List<UserBattleInfo>();
            battleActive = false;

        }
        public virtual int finishResult(List<UserBattleInfo> active_users) { 
            DB.incrementUserWin(DB.nameToUserid(active_users[0].username));
            return DB.nameToUserid(active_users[0].username); ;
        }

        /// <summary>
        /// 1 win, 0 draw, -1 lose
        /// </summary>
        public static int action_eval(BActions action_1, BActions action_2) {
            if (action_1 == BActions.Lizard) {
                if (action_2 == BActions.Lizard) {
                    return 0;
                }
                if (action_2 == BActions.Spock || action_2 == BActions.Paper) {
                    return 1;
                }
                return -1;
            }

            if (action_1 == BActions.Spock) {
                if (action_2 == BActions.Spock) {
                    return 0;
                }
                if (action_2 == BActions.Rock || action_2 == BActions.Scissors) {
                    return 1;
                }
                return -1;
            }

            if (action_1 == BActions.Scissors) {
                if (action_2 == BActions.Scissors) {
                    return 0;
                }
                if (action_2 == BActions.Paper || action_2 == BActions.Lizard) {
                    return 1;
                }
                return -1;
            }

            if (action_1 == BActions.Rock) {
                if (action_2 == BActions.Rock) {
                    return 0;
                }
                if (action_2 == BActions.Scissors || action_2 == BActions.Lizard) {
                    return 1;
                }
                return -1;
            }

            if (action_1 == BActions.Paper) {
                if (action_2 == BActions.Paper) {
                    return 0;
                }
                if (action_2 == BActions.Rock || action_2 == BActions.Spock) {
                    return 1;
                }
                return -1;
            }

            return 0;
        }


        public void fight(UserBattleInfo pA, UserBattleInfo pB) {
            int favorA = 0;
            for (int i = 0; i < 5; i++) {
                log+= pA.username + " vs " + pB.username + "\r\n";
                favorA +=action_eval(pA.actions[i], pB.actions[i]);
                log += "   "+pA.actions[i].ToString() +" vs "+ pB.actions[i] + "\r\n";
            }
            if (favorA > 0) {
                pA.battle_score++;
                log += pA.username + " wins the round! \r\n";
            }
            if (favorA < 0) {
                pB.battle_score++;
                log += pB.username + " wins the round! \r\n";
            }
            if (favorA == 0) {
                log += "A draw?!? A conspiracy?\r\n";
                blocked_users.Add(pA);
                blocked_users.Add(pB);
            }
            log += "\r\n";
        }

       
        }
     

    }
  

