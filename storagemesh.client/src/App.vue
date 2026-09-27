<script setup>
import { onMounted, ref } from "vue";

const files = ref([]);
const nodes = ref([]);
const fileInput = ref(null);
async function loadFiles() {
  const response = await fetch("http://localhost:5004/api/files");
  if (!response.ok) throw new Error("Failed to load files");
  files.value = await response.json();
}

async function loadNodes() {
  const response = await fetch("http://localhost:5004/api/nodes");
  if (!response.ok) throw new Error("Failed to load nodes");
  nodes.value = await response.json();
}

async function loadDashboard() {
  await Promise.all([loadFiles(), loadNodes()]);
}

async function setNodeStatus(node, enabled) {
  const endpoint = enabled ? "on" : "off";
  await fetch(`http://localhost:5004/api/nodes/${node.id}/${endpoint}`, { method: "POST" });
  await loadNodes();
}

async function fetchFile(fileKey) {
  const response = await fetch(
    `http://localhost:5004/api/files/${encodeURIComponent(fileKey)}/fetch`,
  );
  if (!response.ok) {
    alert("File could not be found on the network.");
    return;
  }
  await loadFiles();
}

function openFile(fileKey) {
  window.open(`http://localhost:5004/api/files/${encodeURIComponent(fileKey)}`, "_blank");
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

  await loadFiles();
}

const selectedFile = ref(null);

function selectFile(event) {
  selectedFile.value = event.target.files[0] ?? null;
  fileInput.value = event.target.files[0];
}

async function uploadFile() {
  if (!selectedFile.value) return;

  const file = selectedFile.value;

  const allowedTypes = ["text/plain", "application/pdf", "image/png", "image/jpeg"];

  if (!allowedTypes.includes(file.type)) {
    alert("File type not supported.");
    return;
  }

  if (file.size > 5 * 1024 * 1024) {
    alert("File exceeds the 5 MB limit.");
    return;
  }

  const formData = new FormData();
  formData.append("file", file);

  const response = await fetch("http://localhost:5004/api/files/upload", {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    alert(await response.text());
    return;
  }

  selectedFile.value = null;
  fileInput.value.value = "";
  await loadFiles();
}

onMounted(() => {
  loadDashboard();
  setInterval(loadDashboard, 5000);
});
</script>

<template>
  <main>
    <h1>StorageMesh</h1>
    <section>
      <h2>Nodes</h2>

      <div class="nodes">
        <div v-for="node in nodes" :key="node.id" class="node" :class="node.status">
          <strong>{{ node.id }}</strong>
          <span>{{ node.status }}</span>

          <button v-if="node.status === 'online'" @click="setNodeStatus(node, false)">
            Turn Off
          </button>

          <button v-else @click="setNodeStatus(node, true)">Turn On</button>
        </div>
      </div>
    </section>

    <section>
      <h2>Files</h2>

      <table v-if="files.length">
        <thead>
          <tr>
            <th>File</th>
            <th>Stored At</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>

        <tbody>
          <tr v-for="file in files" :key="file.fileKey" :class="{ missing: !file.exists }">
            <td>{{ file.fileKey }}</td>
            <td>{{ file.storedAt }}</td>
            <td>
              {{ file.exists ? "Available" : "Missing" }}
            </td>
            <td>
              <button :disabled="!file.exists" @click="openFile(file.fileKey)">Open</button>
              <button v-if="file.exists" @click="deleteFile(file.fileKey)">Delete</button>
              <button v-else @click="fetchFile(file.fileKey)">Request</button>
            </td>
          </tr>
        </tbody>
      </table>

      <p v-else>No files found.</p>

      <div class="upload">
        <input
          type="file"
          accept=".txt,.pdf,.png,.jpg,.jpeg"
          @change="selectFile"
          ref="fileInput"
        />

        <button :disabled="!selectedFile" @click="uploadFile">Upload</button>

        <span>Max 5 MB</span>
      </div>
    </section>
  </main>
</template>
