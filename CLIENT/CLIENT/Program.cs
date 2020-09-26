using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Media;

namespace Client
{
    class Program
    {
        // State object for receiving data from remote device.  
        public class StateObject
        {
            // Client socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 256;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];
            // Received data string.  
            public StringBuilder sb = new StringBuilder();
        }

        public class AsynchronousClient
        {
            // The port number for the remote device.  
            private const int port = 11000;

            // ManualResetEvent instances signal completion.  
            private static ManualResetEvent connectDone =
                new ManualResetEvent(false);
            private static ManualResetEvent sendDone =
                new ManualResetEvent(false);
            private static ManualResetEvent receiveDone =
                new ManualResetEvent(false);

            // The response from the remote device.  
            private static String response = String.Empty;
            private static string msg;

            private static void StartClient()
            {
                // Connect to a remote device.  
                try
                {


                    IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                    IPAddress ipAddress = ipHostInfo.AddressList[0];
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                    // Create a TCP/IP socket.  
                    Socket client = new Socket(ipAddress.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);

                    // Connect to the remote endpoint.  
                    client.BeginConnect(remoteEP,
                        new AsyncCallback(ConnectCallback), client);
                    connectDone.WaitOne();


                    // Send login
                    Console.Write("Enter username: ");
                    string username = Console.ReadLine();

                    Console.Write("Enter password: ");
                    string password = Console.ReadLine();

                    // Send test data to the remote device.  
                    Send(client, "username: " + username + " password: " + password + "<EOF>");
                    sendDone.WaitOne();


                    //////////////////////  MY AUTHENTICATION   ///////////////////////


                    if (username.Equals("LOGIN1234") && password.Equals("1234"))
                    {
                        Send(client, "...ACCESS GRANTED.....WELCOME.., " + username);
                    }

                    else
                    {
                        Send(client, "Login Failed");
                        System.Environment.Exit(1);                  /// if invalid login EXIT
                    }


                    Console.Write("Loading...... Prees ENTER to retrieve files");
                    string request = Console.ReadLine();
                    sendDone.WaitOne();

                    Console.Write("Files successfully Obtained........ENTER to open file");
                    string requestfile = Console.ReadLine();

                    ////// LIST OF FILES ///////
                    Console.WriteLine(".................................LIST OF AUDIO FILES.........................................");
                    Console.WriteLine(" 1:           Wanna go shopping ");
                    Console.WriteLine(" 2:           Yummy ");
                    Console.WriteLine(" 3:           Thats it");

                   
                    Console.Write("Send a request: ");
                    msg = Console.ReadLine();
                        

                    if (request.ToLower() == "exit")
                    {
                    System.Environment.Exit(1);
                    }
                    

                    ///////////////////////// PLAY AUDIO ///////////////////////////

                    var myPlayer = new System.Media.SoundPlayer();



                          if (msg == "Play")

                          {

                          myPlayer.SoundLocation = @"C:\Users\100430264\source\repos\SERVER\SERVER\bin\Debug\wanna-go-shopping.WAV";

                          myPlayer.Play();

                          }

                          if (msg == "stop")

                          {

                          myPlayer.Stop();

                        myPlayer.Dispose();

                          }



                    // Receive the response from the remote device.  
                    Receive(client);
                    receiveDone.WaitOne();


                    // Get list of wav files
                    // E.g. Get List
                    string input = Console.ReadLine();
                    Send(client, input + "<EOF>");
                    sendDone.WaitOne();

                    // Receive the response from the remote device.  
                    Receive(client);
                    receiveDone.WaitOne();

                    // Write the response to the console.  
                    Console.WriteLine("Response received : {0}", response);

                    // Release the socket.  
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();

                }
                catch
            (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            private static void ConnectCallback(IAsyncResult ar)
            {
                try
                {
                    // Retrieve the socket from the state object.  
                    Socket client = (Socket)ar.AsyncState;

                    // Complete the connection.  
                    client.EndConnect(ar);

                    Console.WriteLine("Socket connected to {0}",
                        client.RemoteEndPoint.ToString());

                    // Signal that the connection has been made.  
                    connectDone.Set();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            private static void Receive(Socket client)
            {
                try
                {
                    // Create the state object.  
                    StateObject state = new StateObject();
                    state.workSocket = client;

                    // Begin receiving the data from the remote device.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            private static void ReceiveCallback(IAsyncResult ar)
            {
                try
                {
                    // Retrieve the state object and the client socket   
                    // from the asynchronous state object.  
                    StateObject state = (StateObject)ar.AsyncState;
                    Socket client = state.workSocket;

                    // Read data from the remote device.  
                    int bytesRead = client.EndReceive(ar);

                    if (bytesRead > 0)
                    {

                        state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                        // Get the rest of the data.  
                        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReceiveCallback), state);
                    }
                    else
                    {

                        if (state.sb.Length > 1)
                        {
                            response = state.sb.ToString();
                        }
                        // Signal that all bytes have been received.  
                        receiveDone.Set();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            private static void Send(Socket client, String data)
            {
                // Convert the string data to byte data using ASCII encoding.  
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.  
                client.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), client);
            }

            private static void SendCallback(IAsyncResult ar)
            {
                try
                {
                    // Retrieve the socket from the state object.  
                    Socket client = (Socket)ar.AsyncState;

                    // Complete sending the data to the remote device.  
                    int bytesSent = client.EndSend(ar);
                    Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                    // Signal that all bytes have been sent.  
                    sendDone.Set();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            public static int Main(String[] args)
            {
                StartClient();
                return 0;
            }
        }
    }
}
