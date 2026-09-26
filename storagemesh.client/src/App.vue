<script setup>
import { onMounted, ref } from "vue";

const files = ref([]);
const nodes = ref([]);

async function loadFiles() {
  const response = await fetch("http://localhost:5004/api/files");

  if (!response.ok) {
    throw new Error("Failed to load files");
  }

  files.value = await response.json();
}

async function loadNodes() {
  const response = await fetch("http://localhost:5004/api/nodes");

  if (!response.ok) {
    throw new Error("Failed to load nodes");
  }

  nodes.value = await response.json();
}

async function loadDashboard() {
  await Promise.all([loadFiles(), loadNodes()]);
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
          </tr>
        </thead>

        <tbody>
          <tr v-for="file in files" :key="file.fileKey" :class="{ missing: !file.exists }">
            <td>{{ file.fileKey }}</td>
            <td>{{ file.storedAt }}</td>
            <td>
              {{ file.exists ? "Available" : "Missing" }}
            </td>
          </tr>
        </tbody>
      </table>

      <p v-else>No files found.</p>
    </section>
  </main>
</template>
