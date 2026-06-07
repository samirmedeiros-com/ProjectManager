using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.DTOs;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

[Route("api/projects/{projectId}/tasks")]
[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectTaskDto>>> GetProjectTasks(int projectId)
    {
        var tasks = await _taskService.GetTasksByProject(projectId);
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectTaskDto>> GetTask(int projectId, int id)
    {
        var task = await _taskService.GetTaskById(id);
        if (task == null || task.ProjectId != projectId)
            return NotFound();

        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectTaskDto>> CreateTask(int projectId, [FromBody] CreateTaskRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var task = await _taskService.CreateTask(projectId, request);
        return CreatedAtAction(nameof(GetTask), new { projectId, id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int projectId, int id, [FromBody] UpdateTaskRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var task = await _taskService.UpdateTask(id, request);
        if (task == null || task.ProjectId != projectId)
            return NotFound();

        return Ok(task);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int projectId, int id)
    {
        var task = await _taskService.GetTaskById(id);
        if (task == null || task.ProjectId != projectId)
            return NotFound();

        var success = await _taskService.DeleteTask(id);
        if (!success)
            return NotFound();

        return NoContent();
    }
}
