class Tank < ApplicationRecord
	belongs_to :nation
	belongs_to :vehicle_type
	belongs_to :tier
	has_many :marks
end
