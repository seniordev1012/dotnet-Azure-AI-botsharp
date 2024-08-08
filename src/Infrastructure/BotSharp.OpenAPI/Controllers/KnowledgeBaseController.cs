using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.Knowledges.Settings;
using BotSharp.OpenAPI.ViewModels.Knowledges;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeService _knowledgeService;
    private readonly IServiceProvider _services;

    public KnowledgeBaseController(IKnowledgeService knowledgeService, IServiceProvider services)
    {
        _knowledgeService = knowledgeService;
        _services = services;
    }

    [HttpGet("/knowledge/{agentId}")]
    public async Task<List<RetrievedResult>> RetrieveKnowledge([FromRoute] string agentId, [FromQuery(Name = "q")] string question)
    {
        return await _knowledgeService.GetAnswer(new KnowledgeRetrievalModel
        {
            AgentId = agentId,
            Question = question
        });
    }

    [HttpPost("/knowledge-base/upload")]
    public async Task<IActionResult> UploadKnowledge(IFormFile file, [FromQuery] int? startPageNum, [FromQuery] int? endPageNum)
    {
        var setttings = _services.GetRequiredService<KnowledgeBaseSettings>();
        var textConverter = _services.GetServices<IPdf2TextConverter>()
            .First(x => x.GetType().FullName.EndsWith(setttings.Pdf2TextConverter));

        var filePath = Path.GetTempFileName();
        using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var content = await textConverter.ConvertPdfToText(filePath, startPageNum, endPageNum);

        // Process uploaded files
        // Don't rely on or trust the FileName property without validation.

        // Add FeedWithMetaData
        await _knowledgeService.EmbedKnowledge(new KnowledgeCreationModel
        {
            Content = content
        });

        return Ok(new { count = 1, file.Length });
    }

    [HttpPost("/knowledge/{agentId}")]
    public async Task<IActionResult> FeedKnowledge([FromRoute] string agentId, List<IFormFile> files, [FromQuery] int? startPageNum, [FromQuery] int? endPageNum, [FromQuery] bool? paddleModel)
    {
        var setttings = _services.GetRequiredService<KnowledgeBaseSettings>();
        var textConverter = _services.GetServices<IPdf2TextConverter>().First(x => x.GetType().FullName.EndsWith(setttings.Pdf2TextConverter));
        long size = files.Sum(f => f.Length);

        foreach (var formFile in files)
        {
            var filePath = Path.GetTempFileName();


            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await formFile.CopyToAsync(stream);
                await stream.FlushAsync(); // Ensure all data is written to the file
            }

            var content = await textConverter.ConvertPdfToText(filePath, startPageNum, endPageNum);

            // Process uploaded files
            // Don't rely on or trust the FileName property without validation.

            // Add FeedWithMetaData
            await _knowledgeService.Feed(new KnowledgeFeedModel
            {
                AgentId = agentId,
                Content = content
            });

            // Delete the temp file after processing to clean up
            System.IO.File.Delete(filePath);
        }

        return Ok(new { count = files.Count, size });
    }


    [HttpPost("/knowledge/{collection}/data")]
    public async Task<StringIdPagedItems<KnowledgeCollectionDataViewModel>> GetKnowledgeCollectionData([FromRoute] string collection, [FromBody] KnowledgeFilter filter)
    {
        var data = await _knowledgeService.GetKnowledgeCollectionData(collection, filter);
        var items = data.Items?.Select(x => KnowledgeCollectionDataViewModel.ToViewModel(x))?
                               .ToList() ?? new List<KnowledgeCollectionDataViewModel>();

        return new StringIdPagedItems<KnowledgeCollectionDataViewModel>
        {
            Count = data.Count,
            NextId = data.NextId,
            Items = items
        };
    }

    [HttpPost("/knowledge/{collection}/similar")]
    public async Task<IEnumerable<KnowledgeCollectionDataViewModel>> GetSimilarKnowledgeData([FromRoute] string collection, [FromBody] KnowledgeFilter filter)
    {
        var data = await _knowledgeService.GetKnowledgeCollectionData(collection, filter);
        var items = data.Items?.Select(x => KnowledgeCollectionDataViewModel.ToViewModel(x))?
                               .ToList() ?? new List<KnowledgeCollectionDataViewModel>();

        return
    }

    [HttpDelete("/knowledge/{collection}/data/{id}")]
    public async Task<bool> DeleteKnowledgeCollectionData([FromRoute] string collection, [FromRoute] string id)
    {
        return await _knowledgeService.DeleteKnowledgeCollectionData(collection, id);
    }
}
