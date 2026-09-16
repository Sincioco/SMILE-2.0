"""Expand each illustration's base clip and optional alternate renders."""


def clips_for(item):
    base = {key: value for key, value in item.items() if key != 'variants'}
    return [base] + [dict(base, **variant) for variant in item.get('variants', [])]


def all_clips(items):
    return [clip for item in items for clip in clips_for(item)]
