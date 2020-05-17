import { ShardingManager } from 'discord.js'
import config from 'config'

const shards = new ShardingManager('shard.js', {
  token: config.get('token'),
  respawn: true
})

shards.spawn()
