<script setup>
import { watch } from "vue";
import { EditorContent, useEditor } from "@tiptap/vue-3";
import StarterKit from "@tiptap/starter-kit";
import Underline from "@tiptap/extension-underline";
import Link from "@tiptap/extension-link";
import Image from "@tiptap/extension-image";
import Placeholder from "@tiptap/extension-placeholder";
import { TextStyle } from "@tiptap/extension-text-style";
import Color from "@tiptap/extension-color";
import {
  Bold,
  Code2,
  Eraser,
  Heading1,
  Heading2,
  Image as ImageIcon,
  Italic,
  Link as LinkIcon,
  List,
  ListOrdered,
  Quote,
  Redo2,
  Strikethrough,
  Underline as UnderlineIcon,
  Undo2
} from "lucide-vue-next";

const props = defineProps({
  modelValue: {
    type: String,
    default: ""
  },
  placeholder: {
    type: String,
    default: "开始编写你的文章..."
  }
});

const emit = defineEmits(["update:modelValue"]);

const editor = useEditor({
  content: props.modelValue || "",
  extensions: [
    StarterKit,
    Underline,
    TextStyle,
    Color,
    Image.configure({
      inline: false,
      allowBase64: true
    }),
    Link.configure({
      openOnClick: false,
      autolink: true,
      defaultProtocol: "https"
    }),
    Placeholder.configure({
      placeholder: props.placeholder
    })
  ],
  editorProps: {
    attributes: {
      class: "rich-editor-content markdown-body"
    }
  },
  onUpdate: ({ editor }) => {
    emit("update:modelValue", editor.getHTML());
  }
});

watch(
  () => props.modelValue,
  (value) => {
    if (!editor.value) return;
    if (editor.value.getHTML() === value) return;
    editor.value.commands.setContent(value || "", false);
  }
);

function run(command) {
  if (!editor.value) return;
  command(editor.value.chain().focus()).run();
}

function setLink() {
  if (!editor.value) return;
  const previousUrl = editor.value.getAttributes("link").href || "";
  const url = window.prompt("请输入链接地址", previousUrl);

  if (url === null) return;
  if (!url.trim()) {
    editor.value.chain().focus().extendMarkRange("link").unsetLink().run();
    return;
  }

  editor.value.chain().focus().extendMarkRange("link").setLink({ href: url.trim() }).run();
}

function addImage() {
  if (!editor.value) return;
  const url = window.prompt("请输入图片 URL");
  if (!url?.trim()) return;
  editor.value.chain().focus().setImage({ src: url.trim() }).run();
}

function setColor(event) {
  if (!editor.value) return;
  editor.value.chain().focus().setColor(event.target.value).run();
}
</script>

<template>
  <div class="rich-editor">
    <div v-if="editor" class="rich-toolbar" aria-label="编辑工具栏">
      <button type="button" class="icon-btn" title="撤销" @click="run((chain) => chain.undo())">
        <Undo2 :size="18" />
      </button>
      <button type="button" class="icon-btn" title="重做" @click="run((chain) => chain.redo())">
        <Redo2 :size="18" />
      </button>

      <span class="toolbar-divider"></span>

      <button
        type="button"
        class="icon-btn"
        :class="{ active: editor.isActive('heading', { level: 1 }) }"
        title="一级标题"
        @click="run((chain) => chain.toggleHeading({ level: 1 }))"
      >
        <Heading1 :size="18" />
      </button>
      <button
        type="button"
        class="icon-btn"
        :class="{ active: editor.isActive('heading', { level: 2 }) }"
        title="二级标题"
        @click="run((chain) => chain.toggleHeading({ level: 2 }))"
      >
        <Heading2 :size="18" />
      </button>

      <span class="toolbar-divider"></span>

      <button
        type="button"
        class="icon-btn"
        :class="{ active: editor.isActive('bold') }"
        title="加粗"
        @click="run((chain) => chain.toggleBold())"
      >
        <Bold :size="18" />
      </button>
      <button
        type="button"
        class="icon-btn"
        :class="{ active: editor.isActive('italic') }"
        title="斜体"
        @click="run((chain) => chain.toggleItalic())"
      >
        <Italic :size="18" />
      </button>
      <button
        type="button"
        class="icon-btn"
        :class="{ active: editor.isActive('underline') }"
        title="下划线"
        @click="run((chain) => chain.toggleUnderline())"
      >
        <UnderlineIcon :size="18" />
      </button>
      <button
        type="button"
        class="icon-btn"
        :class="{ active: editor.isActive('strike') }"
        title="删除线"
        @click="run((chain) => chain.toggleStrike())"
      >
        <Strikethrough :size="18" />
      </button>

      <label class="color-picker" title="文字颜色">
        <input type="color" value="#2f7df6" @input="setColor" />
      </label>

      <span class="toolbar-divider"></span>

      <button
        type="button"
        class="icon-btn"
        :class="{ active: editor.isActive('bulletList') }"
        title="无序列表"
        @click="run((chain) => chain.toggleBulletList())"
      >
        <List :size="18" />
      </button>
      <button
        type="button"
        class="icon-btn"
        :class="{ active: editor.isActive('orderedList') }"
        title="有序列表"
        @click="run((chain) => chain.toggleOrderedList())"
      >
        <ListOrdered :size="18" />
      </button>
      <button
        type="button"
        class="icon-btn"
        :class="{ active: editor.isActive('blockquote') }"
        title="引用"
        @click="run((chain) => chain.toggleBlockquote())"
      >
        <Quote :size="18" />
      </button>
      <button
        type="button"
        class="icon-btn"
        :class="{ active: editor.isActive('codeBlock') }"
        title="代码块"
        @click="run((chain) => chain.toggleCodeBlock())"
      >
        <Code2 :size="18" />
      </button>

      <span class="toolbar-divider"></span>

      <button type="button" class="icon-btn" title="插入链接" @click="setLink">
        <LinkIcon :size="18" />
      </button>
      <button type="button" class="icon-btn" title="插入图片" @click="addImage">
        <ImageIcon :size="18" />
      </button>
      <button type="button" class="icon-btn" title="清除格式" @click="run((chain) => chain.clearNodes().unsetAllMarks())">
        <Eraser :size="18" />
      </button>
    </div>

    <EditorContent :editor="editor" />
  </div>
</template>
