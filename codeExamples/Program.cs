using System;
using System.Collections.Generic;
using System.Linq;

namespace OSScheduling
{
    public class Job
    {
        public string Name { get; set; } = "";
        public int ArrivalTime { get; set; }
        public int ExecutionTime { get; set; }
        public int Priority { get; set; }

        // Mutated during simulation
        public int RemainingTime { get; set; }
        public int FinishTime { get; set; } = -1;

        public override string ToString()
            => $"Job {Name}, Priority {Priority}, Arrival Time {ArrivalTime}, Execution Time {ExecutionTime}";
    }

    public record GanttSlice(string Name, int Start, int End);

    public static class Program
    {
        public static void Main()
        {
            // 1. Long-term scheduling - admission filter
            Console.WriteLine("===== 1. LONG-TERM SCHEDULING =====\n");
            LongTermScheduling();

            // 2. Short-term: FCFS
            Console.WriteLine("\n===== 2. SHORT-TERM SCHEDULING: FCFS =====\n");
            var fcfsJobs = BuildTable2();
            var fcfsSlices = FCFS(fcfsJobs);
            PrintMetrics(fcfsJobs);
            PrintGantt(fcfsSlices);

            // 2. Short-term: Round Robin with varying quanta
            foreach (int tq in new[] { 1, 3, 4, 6 })
            {
                Console.WriteLine($"\n===== 2. ROUND-ROBIN, TQ = {tq} =====\n");
                var jobs = BuildTable2();
                var slices = RoundRobin(jobs, tq);
                PrintMetrics(jobs);
                PrintGantt(slices);
            }

            // 3. Context switching: priority-based RR against Table 1
            foreach (int tq in new[] { 1, 6 })
            {
                Console.WriteLine($"\n===== 3. PRIORITY ROUND-ROBIN (Table 1), TQ = {tq} =====\n");
                var jobs = BuildTable1();
                var slices = PriorityRoundRobin(jobs, tq);
                PrintMetrics(jobs);
                PrintGantt(slices);
            }
        }

        // Table 1 - includes priority field; used for long-term + priority RR
        static List<Job> BuildTable1() => new()
        {
            new Job { Name = "A", ArrivalTime = 0,  ExecutionTime = 3, Priority = 5  },
            new Job { Name = "B", ArrivalTime = 2,  ExecutionTime = 6, Priority = 4  },
            new Job { Name = "C", ArrivalTime = 5,  ExecutionTime = 5, Priority = 8  },
            new Job { Name = "D", ArrivalTime = 6,  ExecutionTime = 3, Priority = 6  },
            new Job { Name = "E", ArrivalTime = 8,  ExecutionTime = 6, Priority = 10 },
            new Job { Name = "F", ArrivalTime = 9,  ExecutionTime = 2, Priority = 3  },
            new Job { Name = "G", ArrivalTime = 10, ExecutionTime = 6, Priority = 7  }
        };

        // Table 2 - same processes without priority; used for FCFS / RR
        static List<Job> BuildTable2() => BuildTable1()
            .Select(j => new Job
            {
                Name = j.Name,
                ArrivalTime = j.ArrivalTime,
                ExecutionTime = j.ExecutionTime,
                Priority = 0
            }).ToList();

        static void LongTermScheduling()
        {
            var jobs = BuildTable1();
            // Admission rule: only admit jobs above a priority threshold
            var admitted = jobs.Where(job => job.Priority > 5).ToList();

            Console.WriteLine("Admitted jobs (Priority > 5):");
            foreach (var job in admitted) Console.WriteLine($"  {job}");
        }

        static List<GanttSlice> FCFS(List<Job> jobs)
        {
            var ordered = jobs.OrderBy(j => j.ArrivalTime).ToList();
            var slices = new List<GanttSlice>();
            int t = 0;

            foreach (var job in ordered)
            {
                // CPU is idle until the next job arrives (handles arrival gaps)
                if (t < job.ArrivalTime) t = job.ArrivalTime;

                slices.Add(new GanttSlice(job.Name, t, t + job.ExecutionTime));
                t += job.ExecutionTime;
                job.FinishTime = t;
                job.RemainingTime = 0;
            }
            return slices;
        }

        static List<GanttSlice> RoundRobin(List<Job> jobs, int quantum)
        {
            // Reset remaining time for each run
            foreach (var j in jobs) j.RemainingTime = j.ExecutionTime;

            // pending = jobs that haven't arrived yet (sorted by arrival)
            // ready   = FIFO queue of jobs ready to run
            var pending = jobs.OrderBy(j => j.ArrivalTime).ToList();
            var ready = new Queue<Job>();
            var slices = new List<GanttSlice>();
            int t = 0;

            // Move any jobs that have arrived by `time` into the ready queue
            void Admit(int time)
            {
                while (pending.Count > 0 && pending[0].ArrivalTime <= time)
                {
                    ready.Enqueue(pending[0]);
                    pending.RemoveAt(0);
                }
            }

            Admit(t);
            // If nothing's arrived yet, fast-forward to first arrival
            if (ready.Count == 0 && pending.Count > 0)
            {
                t = pending[0].ArrivalTime;
                Admit(t);
            }

            while (ready.Count > 0 || pending.Count > 0)
            {
                if (ready.Count == 0)
                {
                    // Idle - skip ahead to next arrival
                    t = pending[0].ArrivalTime;
                    Admit(t);
                    continue;
                }

                var current = ready.Dequeue();
                int run = Math.Min(quantum, current.RemainingTime);
                slices.Add(new GanttSlice(current.Name, t, t + run));

                // Admit any jobs that arrive DURING this quantum, so they
                // are queued ahead of the (about-to-be-preempted) current job
                for (int step = 1; step <= run; step++) Admit(t + step);

                t += run;
                current.RemainingTime -= run;

                if (current.RemainingTime == 0)
                    current.FinishTime = t;
                else
                    ready.Enqueue(current);   // back of queue if not finished
            }

            return MergeAdjacent(slices);
        }

        static List<GanttSlice> PriorityRoundRobin(List<Job> jobs, int quantum)
        {
            foreach (var j in jobs) j.RemainingTime = j.ExecutionTime;

            var pending = jobs.OrderBy(j => j.ArrivalTime).ToList();
            var ready = new List<Job>();   // not a Queue - we need priority selection
            var slices = new List<GanttSlice>();
            int t = 0;

            void Admit(int time)
            {
                while (pending.Count > 0 && pending[0].ArrivalTime <= time)
                {
                    ready.Add(pending[0]);
                    pending.RemoveAt(0);
                }
            }

            Admit(t);
            if (ready.Count == 0 && pending.Count > 0)
            {
                t = pending[0].ArrivalTime;
                Admit(t);
            }

            while (ready.Count > 0 || pending.Count > 0)
            {
                if (ready.Count == 0)
                {
                    t = pending[0].ArrivalTime;
                    Admit(t);
                    continue;
                }

                // Highest priority value wins
                var current = ready.OrderByDescending(j => j.Priority).First();
                ready.Remove(current);

                int run = Math.Min(quantum, current.RemainingTime);
                slices.Add(new GanttSlice(current.Name, t, t + run));

                for (int step = 1; step <= run; step++) Admit(t + step);

                t += run;
                current.RemainingTime -= run;

                if (current.RemainingTime == 0)
                    current.FinishTime = t;
                else
                    ready.Add(current);
            }

            return MergeAdjacent(slices);
        }

        // Merge adjacent same-name slices into one - cleaner Gantt output
        static List<GanttSlice> MergeAdjacent(List<GanttSlice> slices)
        {
            var merged = new List<GanttSlice>();
            foreach (var s in slices)
            {
                if (merged.Count > 0 &&
                    merged[^1].Name == s.Name &&
                    merged[^1].End == s.Start)
                {
                    merged[^1] = merged[^1] with { End = s.End };
                }
                else
                {
                    merged.Add(s);
                }
            }
            return merged;
        }

        static void PrintMetrics(List<Job> jobs)
        {
            Console.WriteLine($"{"Process",-9}{"Arrival",-10}{"Exec",-7}{"Finish",-9}{"TAT",-7}{"NTAT",-7}");
            double sumTAT = 0, sumNTAT = 0;
            foreach (var j in jobs.OrderBy(j => j.Name))
            {
                int tat = j.FinishTime - j.ArrivalTime;
                double ntat = (double)tat / j.ExecutionTime;
                sumTAT += tat;
                sumNTAT += ntat;
                Console.WriteLine($"{j.Name,-9}{j.ArrivalTime,-10}{j.ExecutionTime,-7}{j.FinishTime,-9}{tat,-7}{ntat,-7:F2}");
            }
            Console.WriteLine($"\nAvg Turnaround Time : {sumTAT / jobs.Count:F2}");
            Console.WriteLine($"Avg Normalised TAT  : {sumNTAT / jobs.Count:F2}");
        }

        static void PrintGantt(List<GanttSlice> slices)
        {
            Console.Write("\nGantt: |");
            foreach (var s in slices) Console.Write($" {s.Name,-2}|");
            Console.Write("\n       0");
            foreach (var s in slices) Console.Write($"{s.End,4}");
            Console.WriteLine();
        }
    }
}
