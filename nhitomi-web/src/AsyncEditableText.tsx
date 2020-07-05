import React, { useContext, useState, ReactNode } from 'react'
import { Typography } from 'antd'
import { ProgressContext } from './Progress'
import { NotificationContext } from './NotificationContext'

/** A text that is asynchronously editable. */
export const AsyncEditableText = ({ value, onChange, ignoreOffsets }: {
  value: ReactNode
  onChange: (value: string) => void | Promise<void>

  ignoreOffsets?: boolean
}) => {
  const [hovered, setHovered] = useState(false)
  const [editing, setEditing] = useState(false)
  const [loading, setLoading] = useState(false)

  const { start, stop } = useContext(ProgressContext)
  const { notification: { error } } = useContext(NotificationContext)

  return (
    <span
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}>

      <Typography.Text
        disabled={loading}
        style={{
          ...ignoreOffsets && {
            margin: 0,
            left: 0
          },
          color: 'inherit'
        }}
        editable={!hovered && !editing ? undefined : {
          editing,

          onStart: () => setEditing(true),
          onChange: async value => {
            if (loading) return

            start()
            setLoading(true)

            try {
              await onChange(value)
            }
            catch (e) {
              error(e)
            }
            finally {
              stop()
              setLoading(false)
              setEditing(false)
            }
          }
        }}>

        {value}
      </Typography.Text>
    </span>
  )
}
