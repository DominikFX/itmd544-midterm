import { client } from './generated/client.gen';
import { taskServiceList, taskServiceCreate, taskServiceDelete, taskServiceSummary } from './generated/sdk.gen';

// Point the client at the current origin (same server hosts API and client)
client.setConfig({ baseUrl: window.location.origin });

// DOM elements
const taskTableBody = document.getElementById('task-table-body') as HTMLTableSectionElement;
const summaryOutput = document.getElementById('summary-output') as HTMLPreElement;
const createForm = document.getElementById('create-form') as HTMLFormElement;
const statusMessage = document.getElementById('status-message') as HTMLDivElement;

function showStatus(msg: string, isError = false) {
    statusMessage.textContent = msg;
    statusMessage.style.color = isError ? '#e74c3c' : '#2ecc71';
    setTimeout(() => { statusMessage.textContent = ''; }, 3000);
}

// List all tasks and display them in the table
async function loadTasks() {
    const { data, error } = await taskServiceList();
    if (error) {
        showStatus('Failed to load tasks', true);
        return;
    }
    taskTableBody.innerHTML = '';
    if (data && data.length > 0) {
        data.forEach(task => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${task.id}</td>
                <td>${task.title}</td>
                <td>${task.assignee}</td>
                <td><span class="status-badge status-${task.status.toLowerCase()}">${task.status}</span></td>
                <td>${task.hours}</td>
                <td>${task.dueDate}</td>
                <td><button class="btn-delete" data-id="${task.id}">Delete</button></td>
            `;
            taskTableBody.appendChild(row);
        });
    } else {
        taskTableBody.innerHTML = '<tr><td colspan="7" style="text-align:center;">No tasks found</td></tr>';
    }
}

// Delete a task by ID
async function deleteTask(id: string) {
    const { error } = await taskServiceDelete({ path: { id } });
    if (error) {
        showStatus(`Failed to delete task: ${id}`, true);
        return;
    }
    showStatus('Task deleted successfully');
    await loadTasks();
}

// Create a new task from the form
async function createTask(e: Event) {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const formData = new FormData(form);

    const body = {
        title: formData.get('title') as string,
        assignee: formData.get('assignee') as string,
        status: formData.get('status') as 'Pending' | 'InProgress' | 'Done',
        hours: parseInt(formData.get('hours') as string, 10),
        dueDate: formData.get('dueDate') as string,
    };

    const { error } = await taskServiceCreate({ body });
    if (error) {
        showStatus('Failed to create task', true);
        return;
    }
    showStatus('Task created successfully');
    form.reset();
    await loadTasks();
}

// Get task summary
async function loadSummary() {
    const { data, error } = await taskServiceSummary();
    if (error) {
        showStatus('Failed to load summary', true);
        return;
    }
    summaryOutput.textContent = JSON.stringify(data, null, 2);
}

// Event listeners
createForm.addEventListener('submit', createTask);
document.getElementById('btn-load')!.addEventListener('click', loadTasks);
document.getElementById('btn-summary')!.addEventListener('click', loadSummary);

// Delegate delete button clicks
taskTableBody.addEventListener('click', (e) => {
    const target = e.target as HTMLElement;
    if (target.classList.contains('btn-delete')) {
        const id = target.getAttribute('data-id')!;
        deleteTask(id);
    }
});

// Load tasks on page load
loadTasks();
