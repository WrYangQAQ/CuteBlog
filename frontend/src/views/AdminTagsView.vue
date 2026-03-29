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
    message.value = err?.payload?.message || err.message || "加载标签失败";
  }
}

async function createRow() {
  try {
    await createTagApi({ name: form.name });
    message.value = "标签创建成功";
    form.name = "";
    await loadRows();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "创建标签失败";
  }
}

async function updateRow(row) {
  try {
    await updateTagApi(row.id, row);
    message.value = "标签更新成功";
  } catch (err) {
    message.value = err?.payload?.message || err.message || "更新标签失败";
  }
}

async function removeRow(id) {
  if (!confirm("确定删除此标签吗？")) return;
  try {
    await deleteTagApi(id);
    await loadRows();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "删除标签失败";
  }
}

onMounted(loadRows);
</script>

<template>
  <section class="stack">
    <div class="panel">
      <h2>标签管理</h2>
      <p v-if="message" :class="message.includes('成功') ? 'ok' : 'error'">{{ message }}</p>
      <div class="inline-form">
        <input v-model.trim="form.name" placeholder="标签名" />
        <button class="btn solid" @click="createRow">新增</button>
      </div>
    </div>

    <div class="panel">
      <table class="cute-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>名称</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.id">
            <td>{{ row.id }}</td>
            <td><input v-model="row.name" /></td>
            <td class="table-actions">
              <button class="btn ghost" @click="updateRow(row)">保存</button>
              <button class="btn danger" @click="removeRow(row.id)">删除</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
