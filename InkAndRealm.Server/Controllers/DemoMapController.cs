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
    public async Task<ActionResult<MapDto>> GetDemoMap()
    {
        var map = await _context.Maps
            .Include(m => m.Trees)
            .FirstOrDefaultAsync(m => m.Name == DemoMapName);

        if (map is null)
        {
            return Ok(new MapDto { Name = DemoMapName });
        }

        return Ok(ToDto(map));
    }

    [HttpPost("tree")]
    public async Task<ActionResult<MapDto>> AddTree([FromBody] TreeFeatureDto tree)
    {
        var map = await _context.Maps
            .Include(m => m.Trees)
            .FirstOrDefaultAsync(m => m.Name == DemoMapName);

        if (map is null)
        {
            map = new MapEntity { Name = DemoMapName };
            _context.Maps.Add(map);
        }

        map.Trees.Add(new TreeEntity
        {
            X = tree.X,
            Y = tree.Y,
            TreeType = string.IsNullOrWhiteSpace(tree.TreeType) ? "Oak" : tree.TreeType
        });

        await _context.SaveChangesAsync();
        return Ok(ToDto(map));
    }

    private static MapDto ToDto(MapEntity map)
    {
        return new MapDto
        {
            Id = map.Id,
            Name = map.Name,
            Trees = map.Trees
                .Select(tree => new TreeFeatureDto
                {
                    Id = tree.Id,
                    X = tree.X,
                    Y = tree.Y,
                    TreeType = tree.TreeType
                })
                .ToList()
        };
    }
}
