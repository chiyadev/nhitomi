import { ShardingManager } from 'discord.js'
import config from 'config'

const shards = new ShardingManager('./build/shard.js', {
  token: config.get('token'),
  respawn: true
})

shards.spawn()
