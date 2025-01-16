using Microsoft.AspNetCore.Mvc;
using MyRedisSentinelWebApp.Services;

namespace MyRedisSentinelWebApp.Controllers
{
    [ApiController]
    [Route("api/redis")]
    public class RedisController : ControllerBase
    {
        private readonly IRedisService _redisService;

        public RedisController(IRedisService redisService)
        {
            _redisService = redisService;
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> Get(string key)
        {
            var value = await _redisService.GetStringAsync(key);
            return value != null ? Ok(value) : NotFound("Key not found");
        }

        [HttpPost]
        public async Task<IActionResult> Set([FromBody] KeyValuePair<string, string> kvp)
        {
            await _redisService.SetStringAsync(kvp.Key, kvp.Value);
            return Ok("Key set successfully");
        }
    }
}
