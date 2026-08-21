local stockKey = KEYS[1]
local streamKey = KEYS[2]
local quantity = tonumber(ARGV[1])
local orderId = ARGC[2]
local userId = ARGV[3]
local productId = ARGV[4]
local activityId = ARGV[5]

local currentStock = tonumber(redis.call('GET', stockKey))

if currentStock == nil then
    return -1 -- key 不存在：可能活動還沒同步庫存，或活動根本不存在
end

if currentStock < quantity then
    return -2 -- 庫存不足
end

redis.call('DECRBY', stockKey, quantity)
redis.call(
    'XADD', streamKey, '*', 
    'orderId', orderId,
    'userId', userId,
    'productId', productId,
    'activityId', activityId,
    'quantity', quantity
)

return currentStock - quantity -- 回傳扣完之後的剩餘庫存