using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using akka.actor;
using akka.routing;
using akka.util;
using com.typesafe.config;

namespace PiApprox
{
    class Program
    {
        static void Main( string[ ] args )
        {
            var pi = new Pi();
            pi.Calculate(8, 30000, 40000);
            

            Console.WriteLine("PressAnyKey To Exit;");
            Console.ReadLine();
        }
    }

     public class Pi {
 
  // actors and messages ...
 
  public void Calculate(int nrOfWorkers, int nrOfElements, int nrOfMessages) {
    // Create an Akka system
      var asm = Assembly.GetExecutingAssembly();
      var resourceName = "PiApprox.application.conf";
      string result = "";

      using(Stream stream = asm.GetManifestResourceStream(resourceName))
      using (StreamReader reader = new StreamReader(stream))
      {
          result = reader.ReadToEnd();
      }

      var config = ConfigFactory.parseString(result);
      
    ActorSystem system = ActorSystem.create("PiSystem", config, new ikvm.runtime.AssemblyClassLoader(Assembly.GetExecutingAssembly()));
 
    // create the result listener, which will print the result and shutdown the system
    ActorRef listener = system.actorOf( new Props( new ListenerFactory() ), "listener" );
 
    // create the master
    ActorRef master = system.actorOf(new Props(new MasterFactory(nrOfWorkers, nrOfMessages, nrOfElements, listener)),"master");
 
    // start the calculation
    master.tell(msg: new Calculate());
 
  }
}

public class ListenerFactory : UntypedActorFactory
{
    public object create()
    {
        return new Listener();
    }
}

public class MasterFactory : UntypedActorFactory
{
    private readonly int nrOfWorkers, nrOfMessages, nrOfElements;
    private readonly ActorRef listener;

    public MasterFactory(int nrOfWorkers, int nrOfMessages, int nrOfElements, ActorRef listener)
    {
        this.nrOfWorkers = nrOfWorkers;
        this.nrOfMessages = nrOfMessages;
        this.nrOfElements = nrOfElements;
        this.listener = listener;
    }

    public object create()
    {
        return new Master(nrOfWorkers, nrOfMessages, nrOfElements, listener);
    }
}

    public class Listener : UntypedActor {
  public override void onReceive(Object message)
  {
      var piApproximation = message as PiApproximation;
      if (piApproximation != null) {
      PiApproximation approximation = piApproximation;
      Console.WriteLine( "\n\tPi approximation: \t\t{0}\n\tCalculation time: \t{1}", approximation.getPi( ), approximation.getDuration( ).toMillis() );
      getContext().system().shutdown();
    } else {
      unhandled(message);
    }
  }
    }

    public class Master : UntypedActor {
    private readonly int nrOfMessages;
    private readonly int nrOfElements;
 
    private double pi;
    private int nrOfResults;
    private readonly DateTime start = DateTime.Now;
    
    private readonly ActorRef listener;
    private readonly ActorRef workerRouter;
 
  public Master(int nrOfWorkers, int nrOfMessages, int nrOfElements, ActorRef listener) {
    this.nrOfMessages = nrOfMessages;
    this.nrOfElements = nrOfElements;
    this.listener = listener;

    workerRouter = this.getContext( ).actorOf( new Props( new WorkerFactory() ).withRouter( new RoundRobinRouter( nrOfWorkers ) ),
        "workerRouter");
  }
 
  public override void onReceive(Object message) {
    if (message is Calculate) {
    for (int start = 0; start < nrOfMessages; start++) {
      workerRouter.tell(new Work(start, nrOfElements), getSelf());
    }
  } else if (message is Result) {
    Result result = (Result) message;
    pi += result.getValue();
    nrOfResults += 1;
    if (nrOfResults == nrOfMessages) {
      // Send the result to the listener
        double ticks = DateTime.Now.Subtract(start).TotalMilliseconds;
        java.util.concurrent.TimeUnit tu = java.util.concurrent.TimeUnit.MILLISECONDS;
      Duration duration = Duration.create(ticks, tu);
      listener.tell(new PiApproximation(pi, duration), getSelf());
      // Stops this actor and all its supervised children
      getContext().stop(getSelf());
    }
  } else {
    unhandled(message);
  }
  }

}

    public class WorkerFactory : UntypedActorFactory
    {
        public object create()
        {
            return new Worker();
        }
    }


    public class Worker : UntypedActor {
 
  // calculatePiFor ...
 
  public override void onReceive(Object message) {
    if (message is Work) {
      Work work = (Work) message;
      double result = calculatePiFor(work.getStart(), work.getNrOfElements());
      getSender().tell(new Result(result), getSelf());
    } else {
      unhandled(message);
    }
  }

  public static double calculatePiFor( int start, int nrOfElements )
  {
      double acc = 0.0;
      for ( int i = start * nrOfElements; i <= ( ( start + 1 ) * nrOfElements - 1 ); i++ )
      {
          acc += 4.0 * ( 1 - ( i % 2 ) * 2 ) / ( 2 * i + 1 );
      }
      return acc;
  }
}

    public class Calculate
    {
    }



    public class Work {
  private readonly int start;
  private readonly int nrOfElements;
 
  public Work(int start, int nrOfElements) {
    this.start = start;
    this.nrOfElements = nrOfElements;
  }
 
  public int getStart() {
    return start;
  }
 
  public int getNrOfElements() {
    return nrOfElements;
  }
}
 


    internal static class PiApproximationFactory
    {
        static PiApproximation Create(double pi, Duration duration)
        {
            return new PiApproximation(pi, duration);
        }

        static Result Create(double value)
        {
            return new Result(value);
        }

        static Work Create( int start, int nrOfElements )
        {
            return new Work(start, nrOfElements);
        }
    }

    public class Result {
  private readonly double value;
 
  public Result(double value) {
    this.value = value;
  }
 
  public double getValue() {
    return value;
  }
}

    public class PiApproximation
    {
        private readonly double pi;
        private readonly Duration duration;

        public PiApproximation(double pi, Duration duration)
        {
            this.pi = pi;
            this.duration = duration;
        }

        public double getPi()
        {
            return pi;
        }

        public Duration getDuration()
        {
            return duration;
        }
    }

}
