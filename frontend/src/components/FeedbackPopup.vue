<script setup>
import { AlertCircle, CheckCircle2 } from "lucide-vue-next";
import { hideFeedback, useFeedbackState } from "../stores/feedback";

const feedback = useFeedbackState();
</script>

<template>
  <div v-if="feedback.visible" class="dialog-mask" @click="hideFeedback">
    <div class="dialog-card feedback-card" @click.stop>
      <div class="feedback-head" :class="feedback.type === 'success' ? 'is-success' : 'is-error'">
        <CheckCircle2 v-if="feedback.type === 'success'" :size="20" />
        <AlertCircle v-else :size="20" />
        <h3>{{ feedback.title }}</h3>
      </div>
      <p>{{ feedback.message }}</p>
      <div class="action-row dialog-actions">
        <button class="btn solid" type="button" @click="hideFeedback">确定</button>
      </div>
    </div>
  </div>
</template>
