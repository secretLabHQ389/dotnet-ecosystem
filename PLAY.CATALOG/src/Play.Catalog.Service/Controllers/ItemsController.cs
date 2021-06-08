using System;
using System.Linq; //no definition for Where error needs this
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Service.Dtos;
using Play.Common;
using Play.Catalog.Service.Entities;
using MassTransit;
using Play.Catalog.Contracts;

namespace Play.Catalog.Service.Controllers
{
    // https://localhost:5001/items -
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        //ItemsRepository configures the rest requests to the mongoDB in ItemsRepository definition line 16:
        //then import the rest requests using it for all controllers, ex. GetAsync() is defined in ItemsRepository
        //Simply make it async like in JavaScript if waiting on external database connection to provide restful responses
        private readonly IRepository<Item> itemsRepository;
        //mass transit setup:
        private readonly IPublishEndpoint publishEndpoint;

        //private static int requestCounter = 0;

        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndpoint)
        {
            this.itemsRepository = itemsRepository;
            this.publishEndpoint = publishEndpoint;
        }
        // static prevents re-creating the list each user uses it
        private static readonly List<ItemDto> items = new()
        {
            //public record ItemDto(Guid Id, string Name, string Description, decimal Price, DateTimeOffset CreatedDate);
            new ItemDto(Guid.NewGuid(), "Potion", "Restores a small amount of HP.", 5, DateTimeOffset.UtcNow),
            new ItemDto(Guid.NewGuid(), "Antidote", "Cures poison", 7, DateTimeOffset.UtcNow),
            new ItemDto(Guid.NewGuid(), "Bronze sword", "Deals a small amount of damage", 20, DateTimeOffset.UtcNow),
        };

        // public static List<ItemDto> Items => items;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {

            // wanting to simulate temporal failures:
            // requestCounter++;
            // Console.WriteLine($"Request {requestCounter}: Starting...");

            // if (requestCounter <= 2)
            // {
            //     Console.WriteLine($"Request {requestCounter}: Delaying...");
            //     await Task.Delay(TimeSpan.FromSeconds(10));
            // }

            // if (requestCounter <= 4)
            // {
            //     Console.WriteLine($"Request {requestCounter}: 500 (Internal Server Error).");
            //     return StatusCode(500);
            // }



            var items = (await itemsRepository.GetAllAsync())
                    .Select(item => item.AsDto());

            //Console.WriteLine($"Request {requestCounter}: 200 (OK).");
            return Ok(items);
        }

        // GET /items/{id} -
        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            //refactor into the extension task of returning the object's properties as a DTO:
            //var item = items.Where(item => item.Id == id).SingleOrDefault();
            var item = await itemsRepository.GetAsync(id);

            if (item == null) {
                return NotFound();
            }
            return item.AsDto();
        }

        // POST /items
        [HttpPost]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
        {
            // var item = new ItemDto(Guid.NewGuid(), createItemDto.Name, createItemDto.Description, createItemDto.Price, DateTimeOffset.UtcNow);
            // items.Add(item);
            var item = new Item
            {
                Name = createItemDto.Name,
                Description = createItemDto.Description,
                Price = createItemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow
            };

            await itemsRepository.CreateAsync(item);

            //announce to MassTransit message broker this action:
            await publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));

            return CreatedAtAction(nameof(GetByIdAsync), new {id = item.Id}, item);
        }

        // PUT /items/{id} -
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {

            var existingItem = await itemsRepository.GetAsync(id);

            if (existingItem == null)
            {
                return NotFound();
            }

            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await itemsRepository.UpdateAsync(existingItem);

            //before using asDto() extension to make it an DTO object:
            // var existingItem = items.Where(item => item.Id == id).SingleOrDefault();

            // if (existingItem == null) {
            //     return NotFound();
            // }

            // var updatedItem = existingItem with
            // {
            //     Name = updateItemDto.Name,
            //     Description = updateItemDto.Description,
            //     Price = updateItemDto.Price
            // };

            // var index = items.FindIndex(existingItem => existingItem.Id == id);

            // items[index] = updatedItem;

            //announce to MassTransit message broker this action:
            await publishEndpoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description));

            return NoContent();
        }

        // DELETE /items/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {

            //var index = items.FindIndex(existingItem => existingItem.Id == id);

            // if (index < 0) {
            //     return NotFound();
            // }

            //items.RemoveAt(index);

            var existingItem = await itemsRepository.GetAsync(id);

            if (existingItem == null)
            {
                return NotFound();
            }

            await itemsRepository.RemoveAsync(existingItem.Id);

            //announce to MassTransit message broker this action:
            await publishEndpoint.Publish(new CatalogItemDeleted(id));

            return NoContent();

        }
    }
}