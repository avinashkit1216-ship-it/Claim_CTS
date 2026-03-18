using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Web.ViewModels;

namespace TaskManagement.Web.Controllers;

/// <summary>
/// TaskController for the MVC Web application
/// 
/// This is an alternative to the API Layer.
/// Instead of returning JSON, it returns HTML views.
/// It uses the same service layer, but for web browsers.
/// </summary>
public class TaskController : Controller
{
    private readonly ITaskService _taskService;

    public TaskController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// GET: /Task
    /// Display list of all tasks
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var tasks = await _taskService.GetAllTasksAsync();
            var viewModel = new TaskListViewModel
            {
                Tasks = tasks.ToList()
            };
            return View(viewModel);
        }
        catch (Exception ex)
        {
            return View("Error", new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET: /Task/Details/5
    /// Display details of a specific task
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
                return NotFound("Task not found");

            return View(task);
        }
        catch (Exception ex)
        {
            return View("Error", new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET: /Task/Create
    /// Display create task form
    /// </summary>
    public IActionResult Create()
    {
        return View(new TaskFormViewModel());
    }

    /// <summary>
    /// POST: /Task/Create
    /// Create a new task
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(TaskFormViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(viewModel);

        try
        {
            var createDto = new CreateTaskDto
            {
                Title = viewModel.Title,
                Description = viewModel.Description
            };

            await _taskService.CreateTaskAsync(createDto);
            TempData["SuccessMessage"] = "Task created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(viewModel);
        }
    }

    /// <summary>
    /// GET: /Task/Edit/5
    /// Display edit task form
    /// </summary>
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
                return NotFound("Task not found");

            var viewModel = new TaskFormViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                IsCompleted = task.IsCompleted
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            return View("Error", new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST: /Task/Edit/5
    /// Update an existing task
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Edit(int id, TaskFormViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(viewModel);

        try
        {
            var updateDto = new UpdateTaskDto
            {
                Title = viewModel.Title,
                Description = viewModel.Description
            };

            await _taskService.UpdateTaskAsync(id, updateDto);
            TempData["SuccessMessage"] = "Task updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(viewModel);
        }
    }

    /// <summary>
    /// POST: /Task/Delete/5
    /// Delete a task
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _taskService.DeleteTaskAsync(id);
            TempData["SuccessMessage"] = "Task deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            return View("Error", new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST: /Task/Complete/5
    /// Mark a task as completed
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            await _taskService.MarkTaskAsCompletedAsync(id);
            TempData["SuccessMessage"] = "Task marked as completed!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            return View("Error", new { message = ex.Message });
        }
    }
}
