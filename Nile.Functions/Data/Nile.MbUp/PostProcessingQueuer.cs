using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nile.Utilities;

namespace Nile.MbUp;

/// <summary>
/// A utility for triggering processing of existing posts for social media operations.
/// This can be used for batch operations like re-processing images, updating metadata, etc.
/// 
/// USAGE:
/// This utility can be run via `dotnet run queue-post-processing` from the Nile.MbUp project directory.
/// 
/// The util is friendly to stopping & re-starting, as it processes posts in batches with delays.
/// </summary>
public class PostProcessingQueuer
{
    private readonly ILogger _logger;
    private readonly IMessageBusUtility _messageBusUtility;
    private readonly IAsyncDelayer _delayUtil;
    private readonly int _queueBatchSize;
    private readonly int _queueBatchDelaySeconds;
    private int _failedPostCount;
    private int _queuedPostCount;

    /// <summary>
    /// Creates a post processing queuer with production-tuned throttling defaults.
    /// </summary>
    /// <param name="messageBusUtility">Message bus utility for sending messages.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="delayUtil">Delay utility for throttling.</param>
    /// <param name="queueBatchSize">The number of messages to queue before pausing.</param>
    /// <param name="queueBatchDelaySeconds">Amount of time to pause between each batch.</param>
    public PostProcessingQueuer(
        IMessageBusUtility messageBusUtility, 
        ILogger logger,
        IAsyncDelayer delayUtil, 
        int queueBatchSize = 50, 
        int queueBatchDelaySeconds = 45)
    {
        _messageBusUtility = messageBusUtility;
        _logger = logger;
        _queueBatchSize = queueBatchSize;
        _queueBatchDelaySeconds = queueBatchDelaySeconds;
        _delayUtil = delayUtil;
    }

    public async Task ProcessPostsNeedingProcessing()
    {
        // TODO: Implement when database context is available
        _logger.LogInformation("Post processing queuer - To be implemented with database context.");
        
        /*
        var posts = await _dbContext.Posts
            .OrderBy(p => p.PostId)
            .ToListAsync();

        for (int postIndex = 0; postIndex < posts.Count; postIndex++)
        {
            var post = posts[postIndex];
            if (PostNeedsProcessing(post))
            {
                await QueuePostProcessing(post.PostId);
            }

            if ((_queuedPostCount + 1) % _queueBatchSize == 0)
            {
                _logger.LogInformation("Progress: processed {Index} of {Total} posts, {QueuedCount} queued, {FailedCount} failed, pausing for {DelaySeconds} seconds...",
                    postIndex + 1, posts.Count, _queuedPostCount, _failedPostCount, _queueBatchDelaySeconds);
                
                await _delayUtil.Delay(_queueBatchDelaySeconds * 1000);
            }
        }

        _logger.LogInformation("Completed post queueing. {QueuedCount} posts queued, {FailedCount} failed.",
            _queuedPostCount, _failedPostCount);
        */
        
        await Task.CompletedTask;
    }

    private async Task QueuePostProcessing(Guid postId)
    {
        try
        {
            await _messageBusUtility.SendMessageAsync(Queues.PostUpdated, new PostUpdatedRequest { PostId = postId });
            _queuedPostCount++;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error queueing post processing for post {PostId}", postId);
            _failedPostCount++;
        }
    }
}
