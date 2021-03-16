namespace PlaylistGame
{
    public class UserBattleInfo
    {
        public UserBattleInfo(string Username) {
            actions = new BActions[5];
            for (int i = 0; i < 5; i++) {
                actions[i] = BActions.NULL;
            }
            username = Username;
            battle_score = 0;
        }
        public string username;

        public int round_score;
        public BActions[] actions;
        public int setActions(string input) {
            input = input.ToLower();
            if (input.Length >= 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    switch (input[i])
                    {
                        case 'r': actions[i] = BActions.Rock; break; 
                        case 'p': actions[i] = BActions.Paper; break;
                        case 's': actions[i] = BActions.Scissors; break;
                        case 'l': actions[i] = BActions.Lizard; break;
                        case 'v': actions[i] = BActions.Spock; break;
                        default: return -2;
                    }
                }
                return 0;
            }
            else {
                return -1;
            }
        }
        public int battle_score;
    }
    }
