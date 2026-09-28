<script setup>
import { computed } from "vue";
import { useRoute } from "vue-router";
import DataTable from "@/components/DataTable.vue";
import { fileColumns, eventColumns, nodeColumns } from "@/tables";
import { network } from "@/network";

const route = useRoute();

const node = computed(() => network.value.find((node) => node.id === route.params.id));

async function deleteFile(node, fileKey) {
  const response = await fetch(
    `http://localhost:5004/api/nodes/${node.id}/files/${encodeURIComponent(fileKey)}`,
    {
      method: "DELETE",
    },
  );

  if (!response.ok) {
    alert("File could not be deleted.");
    return;
  }

  await loadNetwork();
}

async function loadNetwork() {
  const response = await fetch("http://localhost:5004/api/network");
  if (response.ok) network.value = await response.json();
}
</script>

<template>
  <div v-if="node">
    <h2>{{ node.id }}</h2>

    <!-- node.files -->
    <DataTable title="Files" :columns="fileColumns" :rows="node.files ?? []">
      <template #actions="{ row }">
        <div class="actions">
          <button class="btn" @click="deleteFile(node, row.fileKey)"><span>Delete</span></button>
        </div>
      </template>
    </DataTable>
    <!-- node.events -->
    <DataTable title="Events" :columns="eventColumns" :rows="node.events ?? []"> </DataTable>
    <!-- node.knownNodes -->
    <DataTable title="Nodes" :columns="nodeColumns" :rows="node.knownNodes ?? []"> </DataTable>
  </div>

  <p v-else>Node not found.</p>
</template>
