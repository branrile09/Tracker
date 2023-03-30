using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


namespace Tracker
{
    internal class People
    {

        Stopwatch timeSinceUpdate = new Stopwatch();

        public int x = 0;
        public int y = 0;

        public string Username = " ";


        public People(int x, int y, string Username)
        {
            this.x = x;
            this.y = y;
            this.Username = Username;
            timeSinceUpdate.Start();
        }

        public People(byte[] body)
        {
            string message = Encoding.UTF8.GetString(body);
            string[] words = message.Split(' ');
            Username = words[0];
            x = int.Parse(words[1]);
            y = int.Parse(words[2]);
            timeSinceUpdate.Start();
        }


        public static byte[] SendLocation(int x, int y, string Username)
        {
            string message = Username + " " + x + " " + y;
            byte[] encoded_message = Encoding.UTF8.GetBytes(message);
            return encoded_message;
        }

        public static byte[] PersonMove(string Move, string Username)
        {
            string message = Username + " " + Move;
            byte[] encoded_message = Encoding.UTF8.GetBytes(message);
            return encoded_message;
        }

        public static byte[] SendQuery(string Username)
        {
            byte[] encoded_message = Encoding.UTF8.GetBytes(Username);
            return encoded_message;
        }


        public void PersonMove(string Move)
        {
            switch (Move)
            {
                case "UP":
                    y++;
                    if (y > Tracker.Program.maxY)
                      y = Tracker.Program.maxY; 
                    break;

                case "DOWN":
                    y--;
                    if (y < 0)
                    y = 0; 
                    break;
                case "LEFT":
                    x--;
                    if (x < 0)
                        x = 0;
                    break;

                case "RIGHT":
                    x++;
                    if (x > Tracker.Program.maxX)
                        x = Tracker.Program.maxX;
                    break;
            }
            timeSinceUpdate.Restart();
        }

        public bool healthCheck()
        {
            if (timeSinceUpdate.Elapsed.TotalSeconds > 5)
            {
                return false;            
            }
            return true;       
        }



    }
}

