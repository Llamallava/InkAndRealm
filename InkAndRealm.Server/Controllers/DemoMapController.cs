using InkAndRealm.Server.Data;
using InkAndRealm.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InkAndRealm.Server.Controllers;

[ApiController]
[Route("api/demo-map")]
public sealed class DemoMapController : ControllerBase
{
    private const string DemoMapName = "Demo Map";
    private readonly DemoMapContext _context;

    public DemoMapController(DemoMapContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<MapDto>> GetDemoMap([FromQuery] int? userId)
    {
        var map = await _context.Maps
            .Include(m => m.Features)
            .ThenInclude(feature => feature.Points)
            .FirstOrDefaultAsync(m => m.Name == DemoMapName && m.UserId == userId);

        if (map is null)
        {
            return Ok(new MapDto { Name = DemoMapName });
        }

        return Ok(ToDto(map));
    }

    [HttpPost("tree")]
    public async Task<ActionResult<MapDto>> AddTree([FromBody] AddTreeRequest request)
    {
        if (request.UserId is null)
        {
            return Unauthorized("Log in to save map changes.");
        }

        var userExists = await _context.Users.AnyAsync(user => user.Id == request.UserId.Value);
        if (!userExists)
        {
            return Unauthorized("Invalid user.");
        }

        if (request.Tree is null)
        {
            return BadRequest("Tree data is required.");
        }

        var map = await _context.Maps
            .Include(m => m.Features)
            .ThenInclude(feature => feature.Points)
            .FirstOrDefaultAsync(m => m.Name == DemoMapName && m.UserId == request.UserId.Value);

        if (map is null)
        {
            map = new MapEntity
            {
                Name = DemoMapName,
                UserId = request.UserId.Value
            };
            _context.Maps.Add(map);
        }

        var treeFeature = new TreeFeatureEntity
        {
            TreeType = Enum.TryParse<TreeType>(request.Tree.TreeType, out var treeType) ? treeType : TreeType.Oak,
            Points =
            {
                new FeaturePointEntity
                {
                    X = request.Tree.X,
                    Y = request.Tree.Y,
                    SortOrder = 0
                }
            }
        };

        map.Features.Add(treeFeature);

        await _context.SaveChangesAsync();
        return Ok(ToDto(map));
    }

    private static MapDto ToDto(MapEntity map)
    {
        return new MapDto
        {
            Id = map.Id,
            Name = map.Name,
            Trees = map.Features
                .OfType<TreeFeatureEntity>()
                .Select(tree =>
                {
                    var point = tree.Points.OrderBy(p => p.SortOrder).FirstOrDefault();
                    return new TreeFeatureDto
                    {
                        Id = tree.Id,
                        X = point?.X ?? 0f,
                        Y = point?.Y ?? 0f,
                        TreeType = tree.TreeType.ToString()
                    };
                })
                .ToList()
        };
    }
}
