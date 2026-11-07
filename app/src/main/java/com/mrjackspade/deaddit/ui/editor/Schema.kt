package com.mrjackspade.deaddit.ui.editor

import com.mrjackspade.deaddit.core.config.DynamicColor

/**
 * A typed description of an editable settings object: its fields, each with the C# app's
 * [EditorDisplay] label, order and description, and how to read and replace it on an immutable
 * value. Stands in for the C# editor's reflection over attributes.
 *
 * Fields show in [Field.order], ties in declaration order, as the C# editor's stable
 * OrderBy over the properties (in the C# declaration order, which the schemas below keep).
 */
class Schema<T>(fields: List<Field<T>>) {
    val fields: List<Field<T>> = fields.sortedBy { it.order }
}

/** Where an edited value lives: read it, or replace it (which writes through to the root). */
class Ref<T>(val get: () -> T, val set: (T) -> Unit)

sealed class Field<O>(val label: String, val order: Int, val description: String?)

class BoolField<O>(label: String, order: Int, description: String?, val get: (O) -> Boolean, val set: (O, Boolean) -> O) : Field<O>(label, order, description)

/** A string; [multiline] is the C# Multiline flag (a four-row text area). */
class TextField<O>(label: String, order: Int, description: String?, val multiline: Boolean, val get: (O) -> String?, val set: (O, String) -> O) : Field<O>(label, order, description)

class IntField<O>(label: String, order: Int, description: String?, val get: (O) -> Int, val set: (O, Int) -> O) : Field<O>(label, order, description)

class DoubleField<O>(label: String, order: Int, description: String?, val get: (O) -> Double, val set: (O, Double) -> O) : Field<O>(label, order, description)

class ColorField<O>(label: String, order: Int, description: String?, val get: (O) -> DynamicColor, val set: (O, DynamicColor) -> O) : Field<O>(label, order, description)

/** An enum, offered as every one of [values], shown by member name as C#'s Enum.ToString(). */
class EnumField<O, E : Enum<E>>(label: String, order: Int, description: String?, val values: List<E>, val get: (O) -> E, val set: (O, E) -> O) : Field<O>(label, order, description) {
    val names: List<String> get() = values.map { it.name }

    fun currentName(owner: O): String = get(owner).name

    fun select(owner: O, index: Int): O = set(owner, values[index])
}

/** Opens a nested editor page for a part of the edited value. */
interface NestedEditor {
    fun <C> open(schema: Schema<C>, ref: Ref<C>)
}

/** A nested object, edited on its own page behind an "Edit" button. */
class ObjectField<O, C>(label: String, order: Int, description: String?, val schema: Schema<C>, val get: (O) -> C, val set: (O, C) -> O) : Field<O>(label, order, description) {
    fun edit(parent: Ref<O>, editor: NestedEditor) {
        editor.open(schema, Ref({ get(parent.get()) }, { parent.set(set(parent.get(), it)) }))
    }
}

/**
 * A list of objects: each row shows [itemLabel] (C#'s item.ToString(), "Item n" when null) with
 * Edit and Remove; "Add" appends [create]() and opens it.
 */
class ListField<O, I>(
    label: String,
    order: Int,
    description: String?,
    val schema: Schema<I>,
    val create: () -> I,
    val itemLabel: (I) -> String?,
    val get: (O) -> List<I>,
    val set: (O, List<I>) -> O,
) : Field<O>(label, order, description) {
    fun labels(owner: O): List<String> = get(owner).mapIndexed { index, item -> itemLabel(item) ?: "Item ${index + 1}" }

    fun edit(parent: Ref<O>, index: Int, editor: NestedEditor) {
        editor.open(schema, itemRef(parent, index))
    }

    fun remove(parent: Ref<O>, index: Int) {
        parent.set(set(parent.get(), get(parent.get()).toMutableList().also { it.removeAt(index) }))
    }

    /** Appends a new item and opens it. */
    fun add(parent: Ref<O>, editor: NestedEditor) {
        val added = get(parent.get()) + create()
        parent.set(set(parent.get(), added))
        edit(parent, added.size - 1, editor)
    }

    private fun itemRef(parent: Ref<O>, index: Int) = Ref(
        { get(parent.get())[index] },
        { item: I -> parent.set(set(parent.get(), get(parent.get()).toMutableList().also { it[index] = item })) },
    )
}

/** Builds a [Schema] in declaration order. */
class SchemaBuilder<O> {
    val fields = mutableListOf<Field<O>>()

    fun bool(label: String, order: Int = 0, description: String? = null, get: (O) -> Boolean, set: (O, Boolean) -> O) {
        fields += BoolField(label, order, description, get, set)
    }

    fun text(label: String, order: Int = 0, description: String? = null, multiline: Boolean = false, get: (O) -> String?, set: (O, String) -> O) {
        fields += TextField(label, order, description, multiline, get, set)
    }

    fun int(label: String, order: Int = 0, description: String? = null, get: (O) -> Int, set: (O, Int) -> O) {
        fields += IntField(label, order, description, get, set)
    }

    fun double(label: String, order: Int = 0, description: String? = null, get: (O) -> Double, set: (O, Double) -> O) {
        fields += DoubleField(label, order, description, get, set)
    }

    fun color(label: String, order: Int = 0, description: String? = null, get: (O) -> DynamicColor, set: (O, DynamicColor) -> O) {
        fields += ColorField(label, order, description, get, set)
    }

    fun <E : Enum<E>> enum(label: String, values: List<E>, order: Int = 0, description: String? = null, get: (O) -> E, set: (O, E) -> O) {
        fields += EnumField(label, order, description, values, get, set)
    }

    fun <C> obj(label: String, schema: Schema<C>, order: Int = 0, description: String? = null, get: (O) -> C, set: (O, C) -> O) {
        fields += ObjectField(label, order, description, schema, get, set)
    }

    fun <I> list(label: String, schema: Schema<I>, create: () -> I, itemLabel: (I) -> String?, order: Int = 0, description: String? = null, get: (O) -> List<I>, set: (O, List<I>) -> O) {
        fields += ListField(label, order, description, schema, create, itemLabel, get, set)
    }
}

fun <O> schema(build: SchemaBuilder<O>.() -> Unit): Schema<O> = Schema(SchemaBuilder<O>().apply(build).fields)
