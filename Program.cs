namespace Tracker
{

    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System.Text;
    using System.Threading.Tasks;

    internal class Program
    {
        public static int maxY = 10;
        public static int maxX = 10;

        static List<Contacts> Contacted = new List<Contacts>();
        static List<People> Peoples = new List<People>();


        static void Main(string[] args)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            string topic = "Positions";
            bool FINISHED = false;


            //connect and join
            ConnectionPersons("Positions", channel);
            ConnectionQuery("Query", channel);


            while (!FINISHED)
            {

                PositionalRenderer();
                Thread.Sleep(100);
                //Console.Clear();

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
                if (RK == "connect")
                {
                    //Console.WriteLine(" person connected");
                    People newGuy = new People(body);
                    Peoples.Add(newGuy);
                }
                else if (RK == "movement")
                {
                    //Console.WriteLine(" person moved");
                    string[] words = message.Split(' ');
                    string Username = words[0];

                    int ID = 0;

                    for (int i = 0; i < Peoples.Count(); i++)
                    {
                        if (Peoples[i].Username == Username)
                        {
                            Peoples[i].PersonMove(words[1]);
                            ID = i;
                            break;
                        }

                    }

                    foreach (People p in Peoples)
                    {
                        if (p.Username == words[0])
                        {
                            continue;
                        }
                        if (p.x == Peoples[ID].x && p.y == Peoples[ID].y)
                        {
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
                routingKey: string.Empty);

            //consuumer listener
            EventingBasicConsumer consumer = new(channel);
            consumer.Received += (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);
                string contacts = "";

                foreach (Contacts c in Contacted)
                {
                    if (c.Username1 == message)
                    {
                        contacts = c.Username2 + " " + contacts;
                    }
                    else if (c.Username2 == message)
                    {
                        contacts = c.Username1 + " " + contacts;
                    }
                }
                PublishCompleted(channel, contacts);


            };

            channel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);
        }

        static void PublishCompleted(IModel channel, string contactsMade)
        {

            byte[] encoded_message = Encoding.UTF8.GetBytes(contactsMade);

            channel.BasicPublish(exchange: "Query",
                routingKey: string.Empty,
                basicProperties: null,
                body: encoded_message);


        }


        static void PositionalRenderer()
        {

            Console.SetCursorPosition(0, 0);
            for (int row = 0; row < maxY; row++)
            {

                for (int col = 0; col < maxX; col++)
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

        }


    }
}

