﻿namespace CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;

public record BatchExtensionQueue
{
    public Guid QueueId { get; set; }
    public string QueueRequest { get; set; } = string.Empty;
    public string QueueStatus { get; set; } = string.Empty;
    public string BatchStatus { get; set; }  = string.Empty;
    public string SubmittedBy { get; set; }  = string.Empty;
    public DateTime SubmittedDate { get; set; } 
    
}