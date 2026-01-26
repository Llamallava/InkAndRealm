using InkAndRealm.Server.Data;
using InkAndRealm.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace InkAndRealm.Server.Controllers;

[ApiController]
[Route("api/demo-map")]
public sealed class DemoMapController : ControllerBase
{
    private const string DefaultMapName = "Untitled Map";
    private readonly DemoMapContext _context;

    public DemoMapController(DemoMapContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MapSummaryDto>>> GetMaps([FromQuery] int? userId, [FromQuery] string? sessionToken)
    {
        if (userId is null && string.IsNullOrWhiteSpace(sessionToken))
        {
            return Ok(Array.Empty<MapSummaryDto>());
        }

        var user = await ResolveUserAsync(sessionToken, userId);
        if (user is null)
        {
            return Unauthorized("Invalid user.");
        }

        var maps = await _context.Maps
            .Where(map => map.UserId == user.Id)
            .OrderBy(map => map.Id)
            .Select(map => new MapSummaryDto
            {
                Id = map.Id,
                Name = map.Name
            })
            .ToListAsync();

        return Ok(maps);
    }

    [HttpGet("{mapId:int}")]
    public async Task<ActionResult<MapDto>> GetMap([FromRoute] int mapId, [FromQuery] int? userId, [FromQuery] string? sessionToken)
    {
        if (userId is null && string.IsNullOrWhiteSpace(sessionToken))
        {
            return Unauthorized("Log in to load maps.");
        }

        var user = await ResolveUserAsync(sessionToken, userId);
        if (user is null)
        {
            return Unauthorized("Invalid user.");
        }

        var map = await _context.Maps
            .Include(m => m.Features)
            .ThenInclude(feature => feature.Points)
            .FirstOrDefaultAsync(m => m.Id == mapId && m.UserId == user.Id);

        if (map is null)
        {
            return NotFound("Map not found.");
        }

        return Ok(ToDto(map));
    }

    [HttpPost("new")]
    public async Task<ActionResult<MapDto>> CreateMap([FromBody] CreateMapRequest request)
    {
        if (request.UserId is null && string.IsNullOrWhiteSpace(request.SessionToken))
        {
            return Unauthorized("Log in to create a new map.");
        }

        var user = await ResolveUserAsync(request.SessionToken, request.UserId);
        if (user is null)
        {
            return Unauthorized("Invalid user.");
        }

        var map = new MapEntity
        {
            Name = DefaultMapName,
            UserId = user.Id
        };

        _context.Maps.Add(map);
        await _context.SaveChangesAsync();

        return Ok(ToDto(map));
    }

    [HttpPost("edits")]
    public async Task<ActionResult<MapDto>> ApplyEdits([FromBody] MapEditsRequest request)
    {
        if (request is null)
        {
            return BadRequest("Edit payload is required.");
        }

        if (request.UserId is null && string.IsNullOrWhiteSpace(request.SessionToken))
        {
            return Unauthorized("Log in to save map changes.");
        }

        var user = await ResolveUserAsync(request.SessionToken, request.UserId);
        if (user is null)
        {
            return Unauthorized("Invalid user.");
        }

        var map = await _context.Maps
            .Include(m => m.Features)
            .ThenInclude(feature => feature.Points)
            .FirstOrDefaultAsync(m => m.Id == request.MapId && m.UserId == user.Id);

        if (map is null)
        {
            return NotFound("Map not found.");
        }

        var hasChanges = false;
        if (request.AddedTrees is not null && request.AddedTrees.Count > 0)
        {
            foreach (var tree in request.AddedTrees)
            {
                map.Features.Add(CreateTreeFeature(tree));
            }

            hasChanges = true;
        }

        if (request.AddedHouses is not null && request.AddedHouses.Count > 0)
        {
            foreach (var house in request.AddedHouses)
            {
                map.Features.Add(CreateHouseFeature(house));
            }

            hasChanges = true;
        }

        if (request.AddedWaterPolygons is not null && request.AddedWaterPolygons.Count > 0)
        {
            foreach (var polygon in request.AddedWaterPolygons)
            {
                map.Features.Add(CreateWaterFeature(polygon));
            }

            hasChanges = true;
        }

        if (request.UpdatedWaterPolygons is not null && request.UpdatedWaterPolygons.Count > 0)
        {
            foreach (var polygon in request.UpdatedWaterPolygons)
            {
                if (polygon is null || polygon.Id <= 0)
                {
                    continue;
                }

                var feature = map.Features
                    .OfType<WaterFeatureEntity>()
                    .FirstOrDefault(existing => existing.Id == polygon.Id);
                if (feature is null)
                {
                    continue;
                }

                feature.ZIndex = polygon.LayerIndex;
                feature.Points.Clear();
                if (polygon.Points is not null)
                {
                    for (var i = 0; i < polygon.Points.Count; i += 1)
                    {
                        var point = polygon.Points[i];
                        feature.Points.Add(new FeaturePointEntity
                        {
                            X = point.X,
                            Y = point.Y,
                            SortOrder = i
                        });
                    }
                }

                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }

        return Ok(ToDto(map));
    }

    [HttpPost("tree")]
    public async Task<ActionResult<MapDto>> AddTree([FromBody] AddTreeRequest request)
    {
        if (request.UserId is null && string.IsNullOrWhiteSpace(request.SessionToken))
        {
            return Unauthorized("Log in to save map changes.");
        }

        var user = await ResolveUserAsync(request.SessionToken, request.UserId);
        if (user is null)
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
            .FirstOrDefaultAsync(m => m.Id == request.MapId && m.UserId == user.Id);

        if (map is null)
        {
            return NotFound("Map not found.");
        }

        map.Features.Add(CreateTreeFeature(request.Tree));

        await _context.SaveChangesAsync();
        return Ok(ToDto(map));
    }

    [HttpPost("house")]
    public async Task<ActionResult<MapDto>> AddHouse([FromBody] AddHouseRequest request)
    {
        if (request.UserId is null && string.IsNullOrWhiteSpace(request.SessionToken))
        {
            return Unauthorized("Log in to save map changes.");
        }

        var user = await ResolveUserAsync(request.SessionToken, request.UserId);
        if (user is null)
        {
            return Unauthorized("Invalid user.");
        }

        if (request.House is null)
        {
            return BadRequest("House data is required.");
        }

        var map = await _context.Maps
            .Include(m => m.Features)
            .ThenInclude(feature => feature.Points)
            .FirstOrDefaultAsync(m => m.Id == request.MapId && m.UserId == user.Id);

        if (map is null)
        {
            return NotFound("Map not found.");
        }

        map.Features.Add(CreateHouseFeature(request.House));

        await _context.SaveChangesAsync();
        return Ok(ToDto(map));
    }

    private static TreeFeatureEntity CreateTreeFeature(TreeFeatureDto tree)
    {
        return new TreeFeatureEntity
        {
            TreeType = Enum.TryParse<TreeType>(tree.TreeType, out var treeType) ? treeType : TreeType.Oak,
            Points =
            {
                new FeaturePointEntity
                {
                    X = tree.X,
                    Y = tree.Y,
                    SortOrder = 0
                }
            }
        };
    }

    private static HouseFeatureEntity CreateHouseFeature(HouseFeatureDto house)
    {
        return new HouseFeatureEntity
        {
            HouseType = Enum.TryParse<HouseType>(house.HouseType, out var houseType) ? houseType : HouseType.Cottage,
            Points =
            {
                new FeaturePointEntity
                {
                    X = house.X,
                    Y = house.Y,
                    SortOrder = 0
                }
            }
        };
    }

    private static WaterFeatureEntity CreateWaterFeature(AreaPolygonDto polygon)
    {
        var feature = new WaterFeatureEntity
        {
            WaterType = WaterType.Lake,
            ZIndex = polygon?.LayerIndex ?? 0
        };

        if (polygon?.Points is not null)
        {
            for (var i = 0; i < polygon.Points.Count; i += 1)
            {
                var point = polygon.Points[i];
                feature.Points.Add(new FeaturePointEntity
                {
                    X = point.X,
                    Y = point.Y,
                    SortOrder = i
                });
            }
        }

        return feature;
    }

    private static MapDto ToDto(MapEntity map)
    {
        var waterPolygons = map.Features
            .OfType<WaterFeatureEntity>()
            .Select(feature => new AreaPolygonDto
            {
                Id = feature.Id,
                FeatureType = "Water",
                LayerIndex = feature.ZIndex,
                Points = feature.Points
                    .OrderBy(point => point.SortOrder)
                    .Select(point => new MapPointDto
                    {
                        X = point.X,
                        Y = point.Y
                    })
                    .ToList()
            })
            .ToList();

        var waterLayers = waterPolygons
            .GroupBy(polygon => polygon.LayerIndex)
            .OrderBy(group => group.Key)
            .Select(group => new AreaLayerDto
            {
                LayerKey = $"water-{group.Key}",
                LayerIndex = group.Key,
                FeatureType = "Water"
            })
            .ToList();

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
                .ToList(),
            Houses = map.Features
                .OfType<HouseFeatureEntity>()
                .Select(house =>
                {
                    var point = house.Points.OrderBy(p => p.SortOrder).FirstOrDefault();
                    return new HouseFeatureDto
                    {
                        Id = house.Id,
                        X = point?.X ?? 0f,
                        Y = point?.Y ?? 0f,
                        HouseType = house.HouseType.ToString()
                    };
                })
                .ToList(),
            AreaLayers = waterLayers,
            AreaPolygons = waterPolygons
        };
    }

    private async Task<UserEntity?> ResolveUserAsync(string? sessionToken, int? userId)
    {
        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            var now = DateTime.UtcNow;
            return await _context.Sessions
                .Where(session => session.Token == sessionToken
                    && (session.ExpiresUtc == null || session.ExpiresUtc > now))
                .Select(session => session.User)
                .FirstOrDefaultAsync();
        }

        if (userId is null)
        {
            return null;
        }

        return await _context.Users.FirstOrDefaultAsync(user => user.Id == userId.Value);
    }
}
