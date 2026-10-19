"""Combine Unity's versioned component header with assembly-derived game fields."""

from UnityPy.helpers.Tpk import get_typetree_node
from UnityPy.helpers.TypeTreeNode import TypeTreeNode


def component_node(obj):
    """Preserve Unity header alignment when decoding a stripped MonoBehaviour."""
    header = get_typetree_node(obj.class_id, obj.version)
    generated = obj.generate_monobehaviour_node()
    fields = {child.m_Name for child in header.m_Children}
    if not fields.issubset({child.m_Name for child in generated.m_Children}):
        raise ValueError("Generated component metadata does not match the Unity header")
    # The generator's m_Enabled lacks Unity's alignment in some stripped assets.
    # Supply an explicit combined tree through UnityPy's supported reader API;
    # neither cached dependency metadata nor the serialized asset is modified.
    return TypeTreeNode(
        m_Level=generated.m_Level,
        m_Type=generated.m_Type,
        m_Name=generated.m_Name,
        m_ByteSize=generated.m_ByteSize,
        m_Version=generated.m_Version,
        m_MetaFlag=generated.m_MetaFlag,
        m_Children=list(header.m_Children)
        + [child for child in generated.m_Children if child.m_Name not in fields],
    )
