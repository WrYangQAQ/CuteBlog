<script setup>
import { onMounted, reactive, ref } from "vue";
import { createTagApi, deleteTagApi, getTagsApi, updateTagApi } from "../api/taxonomy";

const rows = ref([]);
const message = ref("");
const form = reactive({ name: "" });

async function loadRows() {
  try {
    const res = await getTagsApi();
    rows.value = res.data || [];
  } catch (err) {
    rows.value = [];
    message.value = err?.payload?.message || err.message || "鍔犺浇鏍囩澶辫触";
  }
}

async function createRow() {
  try {
    await createTagApi({ name: form.name });
    message.value = "鏍囩鍒涘缓鎴愬姛";
    form.name = "";
    await loadRows();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "鍒涘缓鏍囩澶辫触";
  }
}

async function updateRow(row) {
  try {
    await updateTagApi(row.id, row);
    message.value = "鏍囩鏇存柊鎴愬姛";
  } catch (err) {
    message.value = err?.payload?.message || err.message || "鏇存柊鏍囩澶辫触";
  }
}

async function removeRow(id) {
  if (!confirm("确定删除此标签吗？")) return;
  try {
    await deleteTagApi(id);
    await loadRows();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "鍒犻櫎鏍囩澶辫触";
  }
}

onMounted(loadRows);
</script>

<template>
  <section class="stack">
    <div class="panel">
      <h2>鏍囩绠＄悊</h2>
      <div class="inline-form">
        <input v-model.trim="form.name" placeholder="标签名" />
        <button class="btn solid" @click="createRow">鏂板</button>
      </div>
    </div>

    <div class="panel">
      <table class="cute-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>鍚嶇О</th>
            <th>鎿嶄綔</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.id">
            <td>{{ row.id }}</td>
            <td><input v-model="row.name" /></td>
            <td class="table-actions">
              <button class="btn ghost" @click="updateRow(row)">淇濆瓨</button>
              <button class="btn danger" @click="removeRow(row.id)">鍒犻櫎</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
