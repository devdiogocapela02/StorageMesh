<script setup>
import { onMounted, ref } from "vue";
import DataTable from "@/components/DataTable.vue";
import { fileColumns, eventColumns, nodeColumns } from "@/tables";
import { network } from "@/network";

const files = ref([]);
const events = ref([]);
const knownnodes = ref([]);
const nodes = ref([]);

async function loadFiles() {
  const response = await fetch("http://localhost:5004/api/files");

  files.value = await response.json();
}

async function loadEvents() {
  const response = await fetch("http://localhost:5004/api/events");
  events.value = await response.json();
}

async function loadKnownNodes() {
  const response = await fetch("http://localhost:5004/api/knownnodes");
  knownnodes.value = await response.json();
}

async function loadNodes() {
  const response = await fetch("http://localhost:5004/api/nodes");
  nodes.value = await response.json();
}

async function loadOverview() {
  await loadFiles();
  await loadEvents();
  await loadKnownNodes();
  loadNodes();
}

const selectedFile = ref(null);
const fileInput = ref(null);

function selectFile(event) {
  selectedFile.value = event.target.files[0] ?? null;
  fileInput.value = event.target.files[0];
}

async function uploadFile() {
  if (!selectedFile.value) return;
  const file = selectedFile.value;
  const allowedFileTypes = ["text/plain", "application/pdf", "image/png", "image/jpeg"];
  if (!allowedFileTypes.includes(file.type)) {
    alert("Unsuported file type");
    return;
  }
  if (file.size > 5 * 1024 * 1024) {
    alert("File exceeds 5MB limit.");
    return;
  }
  const formdata = new FormData();
  formdata.append("file", file);

  const response = await fetch("http://localhost:5004/api/files/upload", {
    method: "POST",
    body: formdata,
  });
  if (!response.ok) {
    alert(await response.text());
    return;
  }
  selectedFile.value = null;
  fileInput.value.value = "";
  await loadNetwork();
  await loadFiles();
}

async function loadNetwork() {
  const response = await fetch("http://localhost:5004/api/network");
  if (response.ok) network.value = await response.json();
}

async function setNodeStatus(node) {
  const endpoint = node.health.enabled ? "off" : "on";
  await fetch(`http://localhost:5004/api/nodes/${node.id}/${endpoint}`, { method: "POST" });
  await loadNetwork();
  await loadNodes();
}

async function deleteFile(fileKey) {
  const response = await fetch(
    `http://localhost:5004/api/files/${encodeURIComponent(fileKey)}/physical`,
    {
      method: "DELETE",
    },
  );

  if (!response.ok) {
    alert("File could not be deleted.");
    return;
  }

  await loadNetwork();
  await loadFiles();
}

async function deleteForever(fileKey) {
  const response = await fetch(
    `http://localhost:5004/api/nodes/files/${encodeURIComponent(fileKey)}/everywhere`,
    {
      method: "DELETE",
    },
  );

  if (!response.ok) {
    alert("File could not be deleted.");
    return;
  }

  await loadNetwork();
  await loadFiles();
}

onMounted(() => {
  loadOverview();
});
</script>

<template>
  <div>
    <h2>Dashboard</h2>
    <div>
      <h2>Nodes</h2>
      <section class="nodes">
        <div v-for="node in network" :key="node.id" class="node-card">
          <h3>{{ node.id }}</h3>
          <p>{{ node.health.enabled ? "Online" : "Offline" }}</p>
          <button class="btn" @click="setNodeStatus(node)">
            <span>{{ node.health.enabled ? "Turn off" : "Turn On" }}</span>
          </button>
        </div>
      </section>
    </div>
    <div>
      <h2>Tables</h2>
      <DataTable title="Files" :columns="fileColumns" :rows="files">
        <template #actions="{ row }">
          <div class="actions">
            <button class="btn" v-if="row.exists" @click="openFile(row.fileKey)">
              <span>Open</span>
            </button>
            <button class="btn" v-else @click="fetchFile(row.filename)">
              <span>Request</span>
            </button>
            <button class="btn" @click="deleteFile(row.fileKey)"><span>Delete</span></button>
            <button class="btn" @click="deleteForever(row.fileKey)">
              <span>Delete forever</span>
            </button>
          </div>
        </template>
      </DataTable>
      <div class="upload">
        <h3>Upload new file</h3>
        <section class="upload-actions">
          <input
            style="display: none"
            id="file-upload"
            type="file"
            accept=".txt,.pdf,.png,.jpg,.jpeg"
            @change="selectFile"
            ref="fileInput"
          />
          <label class="btn" for="file-upload"><span>Select a file</span></label>

          <div>
            <button class="btn" :disabled="!selectedFile" @click="uploadFile">
              <span>Upload</span>
            </button>
            <span class="max-size">Max 5 MB</span>
          </div>
        </section>
      </div>
      <DataTable title="Events" :columns="eventColumns" :rows="events" />
      <DataTable title="Known Nodes" :columns="nodeColumns" :rows="knownnodes" />
    </div>
  </div>
</template>
