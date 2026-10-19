"""Resolve shipyard option compatibility, including transitive dependencies."""


class Options:
    """Check whether a set of options can coexist without selecting every boat part."""

    def __init__(self):
        self.groups = {}
        self.requires = {}
        self.disabled = {}

    def compatible(self, *requested):
        pending = [key for key in requested if key is not None]
        selected = set()
        groups = {}
        forbidden = set()
        while pending:
            key = pending.pop()
            if key in selected:
                continue
            selected.add(key)
            if key in forbidden:
                return False
            group = self.groups.get(key)
            if group is not None:
                if group in groups and groups[group] != key:
                    return False
                groups[group] = key
            forbidden.update(self.disabled.get(key, ()))
            if selected & forbidden:
                return False
            pending.extend(self.requires.get(key, ()))
        return True
