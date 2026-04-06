using enBot.Models;
using System.Threading.Tasks;

namespace enBot.Services.Analysis;

public interface IAnalysisService
{
    Task<HookPayload> AnalyzeAsync(string original);
}
