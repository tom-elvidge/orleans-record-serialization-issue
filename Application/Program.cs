using Orleans.Runtime;
using Domain;

[assembly: GenerateCodeForDeclaringAssembly(typeof(UrlDetails))]

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("urls");
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/shorten/{redirect}",
    async (IGrainFactory grains, HttpRequest request, string redirect) =>
    {
        // Create a unique, short ID
        var shortenedRouteSegment = Guid.NewGuid().GetHashCode().ToString("X");

        // Create and persist a grain with the shortened ID and full URL
        var shortenerGrain = grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
        // await shortenerGrain.SetUrl(redirect);

        // pass the UrlDetails via a grain call to illustrate the problem
        var urlDetails = new UrlDetails
        {
            FullUrl = redirect,
            ShortenedRouteSegment = shortenedRouteSegment
        };
        
        // SET A BREAK POINT HERE
        // step into Application.orleans.g.cs to see the DeepCopy method with no implementation
        await shortenerGrain.SetUrlDetails(urlDetails);
        
        // Return the shortened URL for later use
        var resultBuilder = new UriBuilder($"{ request.Scheme }://{ request.Host.Value}")
        {
            Path = $"/go/{shortenedRouteSegment}"
        };

        return Results.Ok(resultBuilder.Uri);
    });

app.MapGet("/go/{shortenedRouteSegment}",
    async (IGrainFactory grains, string shortenedRouteSegment) =>
    {
        // Retrieve the grain using the shortened ID and redirect to the original URL        
        var shortenerGrain = grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
        var url = await shortenerGrain.GetUrl();

        return Results.Redirect(url);
    });

app.Run();

public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task SetUrl(string fullUrl);
    Task<string> GetUrl();
    Task SetUrlDetails(UrlDetails urlDetails);
}
public class UrlShortenerGrain : Grain, IUrlShortenerGrain
{
    private readonly IPersistentState<UrlDetails> _state;

    public UrlShortenerGrain(
        [PersistentState(
                stateName: "url",
                storageName: "urls")]
                IPersistentState<UrlDetails> state)
    {
        _state = state;
    }

    public async Task SetUrl(string fullUrl)
    {
        _state.State = new UrlDetails() { ShortenedRouteSegment = this.GetPrimaryKeyString(), FullUrl = fullUrl };
        await _state.WriteStateAsync();
    }
    
    public Task<string> GetUrl()
    {
        return Task.FromResult(_state.State.FullUrl);
    }
    
    public async Task SetUrlDetails(UrlDetails urlDetails)
    {
        // SET A BREAK POINT HERE
        // urlDetails is not copied correctly, we just get new()
        _state.State = urlDetails;
        await _state.WriteStateAsync();
    }
}