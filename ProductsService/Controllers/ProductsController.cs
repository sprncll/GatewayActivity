using Microsoft.AspNetCore.Mvc;
using ProductsService.Models;

namespace ProductsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly List<Product> _products = new()
    {
        new Product { Id = 1, Name = "Keyboard", Price = 1500 },
        new Product { Id = 2, Name = "USB Cable", Price = 250 }
    };
    private static int _nextId = 3;

    [HttpGet]
    public IActionResult GetAll() => Ok(_products);

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public IActionResult Create(Product product)
    {
        product.Id = _nextId++;
        _products.Add(product);
        return Created($"/api/products/{product.Id}", product);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, Product updated)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product is null) return NotFound();
        product.Name = updated.Name;
        product.Price = updated.Price;
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product is null) return NotFound();
        _products.Remove(product);
        return NoContent();
    }
}
