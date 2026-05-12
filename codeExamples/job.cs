foreach (var job in admittedJobs)
{
    Console.WriteLine(
        $"Job {job.Name}, Priority {job.Priority}, Arrival Time {job.ArrivalTime}, Execution Time {job.ExecutionTime}");
}


