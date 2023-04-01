namespace Tracker
{

    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    internal class Program
    {
        public static int maxY = 10;
        public static int maxX = 10;

        static List<Contacts> Contacted = new List<Contacts>();
        static List<People> Peoples = new List<People>();

        //mutex for fun threads stuffs
        static public Mutex rendererMutex = new Mutex();

        static void Main(string[] args)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            bool FINISHED = false;


            //connect and join
            ConnectionPersons("Positions", channel);
            ConnectionQuery("Query", channel);
            Stopwatch clearScreenTimer = new Stopwatch();
            clearScreenTimer.Start();
            while (!FINISHED)
            {
                PositionalRenderer();                
                Thread.Sleep(100);   
                connectionHealthCheck();

                //clear screen regularly, to prevent weird stuffs
                if (clearScreenTimer.Elapsed.TotalSeconds > 5)
                {
                    clearScreenTimer.Restart();
                    Console.Clear();                
                }


            }

        }

        static void ConnectionPersons(string topic, IModel channel)
        {

            channel.ExchangeDeclare(exchange: topic, type: ExchangeType.Fanout);

            // declare a server-named queue
            var queueName = channel.QueueDeclare().QueueName;

            channel.QueueBind(queue: queueName,
                exchange: topic,
                routingKey: string.Empty);

            //consuumer listener
            EventingBasicConsumer consumer = new(channel);
            consumer.Received += (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);

                string RK = ea.RoutingKey;
                //using routing key to determine different commands
                if (RK == "connect")
                {
                    //create new object, add to list
                    People newGuy = new People(body);
                    Peoples.Add(newGuy);
                }
                else if (RK == "movement")
                {
                    //process movement, 
                    string[] words = message.Split(' ');
                    string Username = words[0];

                    int ID = 0;
                    //find person in list and save ID
                    for (int i = 0; i < Peoples.Count(); i++)
                    {
                        if (Peoples[i].Username == Username)
                        {
                            Peoples[i].PersonMove(words[1]);
                            ID = i;
                            break;
                        }

                    }
                    //now we check for collisions/contact
                    foreach (People p in Peoples)
                    {
                        if (p.Username == words[0])
                        { //same person, we ignore
                            continue;
                        }
                        if (p.x == Peoples[ID].x && p.y == Peoples[ID].y)
                        {// check for collision
                            Contacts newContact = new Contacts(p.Username, Peoples[ID].Username, p.x, p.y);
                            Contacted.Add(newContact);

                        }

                    }



                }

               
            };

            channel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);
        }


        static void ConnectionQuery(string topic, IModel channel)
        {

            channel.ExchangeDeclare(exchange: topic, type: ExchangeType.Fanout);

            // declare a server-named queue
            var queueName = channel.QueueDeclare().QueueName;

            channel.QueueBind(queue: queueName,
                exchange: topic,
                routingKey: String.Empty);

            //consuumer listener
            EventingBasicConsumer consumer = new(channel);
            consumer.Received += (model, ea) =>
            {
                if (ea.RoutingKey == "Request")
                { // routing key request, as i need to send responses that needs to be ignored
                    byte[] body = ea.Body.ToArray();
                    string message = Encoding.UTF8.GetString(body);
                    string contacts = "";
                    //we cycle through list of collisions recorded, then we add person we collided with to the list
                    foreach (Contacts c in Contacted)
                    {
                        if (c.Username1 == message)
                        {
                            contacts = c.Username2 + " " + contacts; //we do it this way to order list correctly
                        }
                        else if (c.Username2 == message)
                        {
                            contacts = c.Username1 + " " + contacts;
                        }
                    }
                    if (contacts.Length > 0)
                    { //we only publish when there is atleast 1 person contacted
                        PublishCompleted(channel, contacts);
                    }
                }

            };

            channel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);
        }

        static void PublishCompleted(IModel channel, string contactsMade)
        {
            //respond to query            
            byte[] encoded_message = Encoding.UTF8.GetBytes(contactsMade);            
            channel.BasicPublish(exchange: "Query",
                routingKey: "Response",
                basicProperties: null,
                body: encoded_message);
        }


        static void PositionalRenderer()
        {
            //literally stupidest way to render people, but it works <insert shrug emoji>
           rendererMutex.WaitOne();
            Console.SetCursorPosition(0, 0);
            for (int row = 0; row < maxY+1; row++)
            {

                for (int col = 0; col < maxX+1; col++)
                {
                    for (int i = 0; i < Peoples.Count(); i++)
                    {
                        if (Peoples[i].x == col && Peoples[i].y == row)
                        {
                            Console.Write(i);

                        }
                        else
                        {
                            Console.Write(" ");
                        }
                    }



                }
                Console.WriteLine();
            }
            Console.WriteLine("Contact count: " + Contacted.Count);
            rendererMutex.ReleaseMutex();
        }


        static void connectionHealthCheck()
        {
            //we are checking connection is good, or person still active
            //if no longer active, we remove them from the list
            for (int i = 0; i < Peoples.Count; i++)
            {
                bool healthy = Peoples[i].healthCheck();
                if (!healthy)
                {
                    Peoples.RemoveAt(i);
                    Console.Clear();
                    i--;              
                
                }
            
            }
        }

    }
}

